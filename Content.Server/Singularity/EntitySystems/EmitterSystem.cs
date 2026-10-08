using System.Numerics;
using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Projectiles;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Projectiles;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Singularity.EntitySystems
{
    public sealed partial class EmitterSystem : SharedEmitterSystem
    {
        [Dependency] private IRobustRandom _random = default!;
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private SharedAppearanceSystem _appearance = default!;
        [Dependency] private SharedPopupSystem _popup = default!;
        [Dependency] private ProjectileSystem _projectile = default!;
        [Dependency] private GunSystem _gun = default!;
        [Dependency] private PowerStateSystem _powerState = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmitterComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
            SubscribeLocalEvent<EmitterComponent, PowerChangedEvent>(OnApcChanged);
            SubscribeLocalEvent<EmitterComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<EmitterComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
            SubscribeLocalEvent<EmitterComponent, SignalReceivedEvent>(OnSignalReceived);
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

            var isOn = _powerState.GetWorkingState(uid);
            if (TryComp(uid, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
            {
                if (!isOn)
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

                var stateText = isOn ? "on" : "off";
                _adminLogger.Add(LogType.FieldGeneration,
                    isOn ? LogImpact.Medium : LogImpact.High,
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
            if (!_powerState.GetWorkingState(uid))
                return;

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
            if (!_powerState.GetWorkingState(uid))
                return;

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
            _powerState.SetWorkingState(uid, false);
            PowerOff(uid, component);
            UpdateAppearance(uid, component);
        }

        public void SwitchOn(EntityUid uid, EmitterComponent component)
        {
            _powerState.SetWorkingState(uid, true);
            if (TryComp<ApcPowerReceiverComponent>(uid, out var apcReceiver))
            {
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
            else if (_powerState.GetWorkingState(uid))
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
                if (_powerState.GetWorkingState(uid))
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
                Dirty(uid, component);
            }
        }
    }
}
