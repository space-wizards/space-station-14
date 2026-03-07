using System.Numerics;
using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Projectiles;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Construction;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Projectiles;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Singularity.EntitySystems
{
    public sealed class EmitterSystem : SharedEmitterSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly ProjectileSystem _projectile = default!;
        [Dependency] private readonly GunSystem _gun = default!;
        [Dependency] private readonly RadioSystem _radio = default!;
        [Dependency] private readonly NavMapSystem _navMap = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmitterComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
            SubscribeLocalEvent<EmitterComponent, PowerChangedEvent>(OnApcChanged);
            SubscribeLocalEvent<EmitterComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<EmitterComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
            SubscribeLocalEvent<EmitterComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<EmitterComponent, DestructionAttemptEvent>(OnDestructionAttempted);
            SubscribeLocalEvent<EmitterComponent, MachineDeconstructedEvent>(OnDeconstructed); // you shouldn't be able to deconstruct locked emitters but out of scope to fix
            SubscribeLocalEvent<EmitterComponent, LockToggledEvent>(OnLockToggled);
        }

        private void OnAnchorStateChanged(EntityUid uid, EmitterComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                return;

            SwitchOff(uid, component);
        }

        private void OnActivate(EntityUid uid, EmitterComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            if (TryComp(uid, out LockComponent? lockComp) && lockComp.Locked)
            {
                _popup.PopupEntity(Loc.GetString("comp-emitter-access-locked",
                    ("target", uid)), uid, args.User);
                return;
            }

            if (TryComp(uid, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
            {
                if (!component.IsOn)
                {
                    SwitchOn(uid, component);
                    _popup.PopupEntity(Loc.GetString("comp-emitter-turned-on",
                        ("target", uid)), uid, args.User);
                }
                else
                {
                    SwitchOff(uid, component);
                    _popup.PopupEntity(Loc.GetString("comp-emitter-turned-off",
                        ("target", uid)), uid, args.User);
                }

                var stateText = component.IsOn ? "on" : "off";
                _adminLogger.Add(LogType.FieldGeneration,
                    component.IsOn ? LogImpact.Medium : LogImpact.High,
                    $"{ToPrettyString(args.User):player} toggled {ToPrettyString(uid):emitter} to {stateText}");
                args.Handled = true;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("comp-emitter-not-anchored",
                    ("target", uid)), uid, args.User);
            }
        }

        private void ReceivedChanged(
            EntityUid uid,
            EmitterComponent component,
            ref PowerConsumerReceivedChanged args)
        {
            if (!component.IsOn)
            {
                return;
            }

            if (args.ReceivedPower < args.DrawRate)
            {
                PowerOff(uid, component);
            }
            else
            {
                PowerOn(uid, component);
            }
        }

        private void OnApcChanged(EntityUid uid, EmitterComponent component, ref PowerChangedEvent args)
        {
            if (!component.IsOn)
            {
                return;
            }

            if (!args.Powered)
            {
                PowerOff(uid, component);
            }
            else
            {
                PowerOn(uid, component);
            }
        }

        public void SwitchOff(EntityUid uid, EmitterComponent component)
        {
            component.IsOn = false;
            if (TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
                powerConsumer.DrawRate = 1; // this needs to be not 0 so that the visuals still work.
            if (TryComp<ApcPowerReceiverComponent>(uid, out var apcReceiver))
                apcReceiver.Load = 1;
            PowerOff(uid, component);
            UpdateAppearance(uid, component);
        }

        public void SwitchOn(EntityUid uid, EmitterComponent component)
        {
            component.IsOn = true;
            if (TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
                powerConsumer.DrawRate = component.PowerUseActive;
            if (TryComp<ApcPowerReceiverComponent>(uid, out var apcReceiver))
            {
                apcReceiver.Load = component.PowerUseActive;
                if (apcReceiver.Powered)
                    PowerOn(uid, component);
            }
            // Do not directly PowerOn().
            // OnReceivedPowerChanged will get fired due to DrawRate change which will turn it on.
            UpdateAppearance(uid, component);
        }

        public void PowerOff(EntityUid uid, EmitterComponent component)
        {
            if (!component.IsPowered)
            {
                return;
            }

            AlertRadio((uid, component), "unpowered");

            component.IsPowered = false;

            // Must be set while emitter powered.
            DebugTools.AssertNotNull(component.TimerCancel);
            component.TimerCancel?.Cancel();

            UpdateAppearance(uid, component);
        }

        public void PowerOn(EntityUid uid, EmitterComponent component)
        {
            if (component.IsPowered)
            {
                return;
            }

            component.IsPowered = true;

            component.FireShotCounter = 0;
            component.TimerCancel = new CancellationTokenSource();

            Timer.Spawn(component.FireBurstDelayMax, () => ShotTimerCallback(uid, component), component.TimerCancel.Token);

            UpdateAppearance(uid, component);
        }

        private void ShotTimerCallback(EntityUid uid, EmitterComponent component)
        {
            if (component.Deleted)
                return;

            // Any power-off condition should result in the timer for this method being cancelled
            // and thus not firing
            DebugTools.Assert(component.IsPowered);
            DebugTools.Assert(component.IsOn);

            Fire(uid, component);

            TimeSpan delay;
            if (component.FireShotCounter < component.FireBurstSize)
            {
                component.FireShotCounter += 1;
                delay = component.FireInterval;
            }
            else
            {
                component.FireShotCounter = 0;
                var diff = component.FireBurstDelayMax - component.FireBurstDelayMin;
                // TIL you can do TimeSpan * double.
                delay = component.FireBurstDelayMin + _random.NextFloat() * diff;
            }

            // Must be set while emitter powered.
            DebugTools.AssertNotNull(component.TimerCancel);
            Timer.Spawn(delay, () => ShotTimerCallback(uid, component), component.TimerCancel!.Token);
        }

        private void Fire(EntityUid uid, EmitterComponent component)
        {
            if (!TryComp<GunComponent>(uid, out var gunComponent))
                return;

            var xform = Transform(uid);
            var ent = Spawn(component.BoltType, xform.Coordinates);
            var proj = EnsureComp<ProjectileComponent>(ent);
            _projectile.SetShooter(ent, proj, uid);

            var targetPos = new EntityCoordinates(uid, new Vector2(0, -1));

            _gun.Shoot((uid, gunComponent), ent, xform.Coordinates, targetPos, out _);
        }

        private void UpdateAppearance(EntityUid uid, EmitterComponent component)
        {
            EmitterVisualState state;
            if (component.IsPowered)
            {
                state = EmitterVisualState.On;
            }
            else if (component.IsOn)
            {
                state = EmitterVisualState.Underpowered;
            }
            else
            {
                state = EmitterVisualState.Off;
            }
            _appearance.SetData(uid, EmitterVisuals.VisualState, state);
        }

        private void OnSignalReceived(EntityUid uid, EmitterComponent component, ref SignalReceivedEvent args)
        {
            // must anchor the emitter for signals to work
            if (TryComp<PhysicsComponent>(uid, out var phys) && phys.BodyType != BodyType.Static)
                return;

            if (args.Port == component.OffPort)
            {
                SwitchOff(uid, component);
            }
            else if (args.Port == component.OnPort)
            {
                SwitchOn(uid, component);
            }
            else if (args.Port == component.TogglePort)
            {
                if (component.IsOn)
                {
                    SwitchOff(uid, component);
                }
                else
                {
                    SwitchOn(uid, component);
                }
            }
            else if (component.SetTypePorts.TryGetValue(args.Port, out var boltType))
            {
                component.BoltType = boltType;
            }
        }

        private void OnDestructionAttempted(Entity<EmitterComponent> ent, ref DestructionAttemptEvent args)
        {
            // warn engineering their containment engine needs IMMEDIATE repairs
            // this doesn't change much for natural loosing through emitter destruction given any meteor warning serves the same purpose
            // can also be used to scare engineering though given it broadcasts its location you need a renamed station beacon to really scare them
            AlertRadio(ent, "destroyed");
        }

        private void OnDeconstructed(Entity<EmitterComponent> ent, ref MachineDeconstructedEvent args)
        {
            // right now you don't even need to unlock the emitter to deconstruct it. that's almost certainly a bug but even without it it probably still needs an alert
            AlertRadio(ent, "deconstructed");
        }

        private void AlertRadio(Entity<EmitterComponent> ent, string type)
        {
            if (!ent.Comp.AlertRadio || !ent.Comp.IsOn || !ent.Comp.IsPowered)
                return; // APEs do not need to scream over engineering radio, and an emitter that is off is probably not going to be alerting radios

            var message = Loc.GetString("emitter-" + type + "-broadcast",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(ent.Owner)))
            );
            _radio.SendRadioMessage(ent.Owner, message, ent.Comp.RadioChannel, ent.Owner);
        }

        private void OnLockToggled(Entity<EmitterComponent> ent, ref LockToggledEvent args)
        {
            if (args.Locked)
                return;

            AlertRadio(ent, "unlocked");
        }
    }
}
