using System.Numerics;
using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Projectiles;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Projectiles;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Singularity.EntitySystems
{
    [UsedImplicitly]
    public sealed class EmitterSystem : SharedEmitterSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly ProjectileSystem _projectile = default!;
        [Dependency] private readonly GunSystem _gun = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmitterComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
            SubscribeLocalEvent<EmitterComponent, PowerChangedEvent>(OnApcChanged);
            SubscribeLocalEvent<EmitterComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<EmitterComponent, GetVerbsEvent<Verb>>(OnGetVerb);
            SubscribeLocalEvent<EmitterComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<EmitterComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
            SubscribeLocalEvent<EmitterComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        private void OnAnchorStateChanged(EntityUid uid, EmitterComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                return;

            SwitchOff(uid, component);
        }

        private void OnInteractHand(EntityUid uid, EmitterComponent component, InteractHandEvent args)
        {
            if (args.Handled)
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

                _adminLogger.Add(LogType.FieldGeneration,
                    component.IsOn ? LogImpact.Medium : LogImpact.High,
                    $"{ToPrettyString(args.User):player} toggled {ToPrettyString(uid):emitter}");
                args.Handled = true;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("comp-emitter-not-anchored",
                    ("target", uid)), uid, args.User);
            }
        }

        private void OnGetVerb(EntityUid uid, EmitterComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanAccess || !args.CanInteract || args.Hands == null)
                return;

            if (TryComp<LockComponent>(uid, out var lockComp) && lockComp.Locked)
                return;

            if (component.SelectableTypes.Count < 2)
                return;

            foreach (var type in component.SelectableTypes)
            {
                var proto = _prototype.Index<EntityPrototype>(type);

                var v = new Verb
                {
                    Priority = 1,
                    Category = VerbCategory.SelectType,
                    Text = proto.Name,
                    Disabled = type == component.BoltType,
                    Impact = LogImpact.Medium,
                    DoContactInteraction = true,
                    Act = () =>
                    {
                        component.BoltType = type;
                        _popup.PopupEntity(Loc.GetString("emitter-component-type-set", ("type", proto.Name)), uid);
                    }
                };
                args.Verbs.Add(v);
            }
        }

        private void OnExamined(EntityUid uid, EmitterComponent component, ExaminedEvent args)
        {
            if (component.SelectableTypes.Count < 2)
                return;
            var proto = _prototype.Index<EntityPrototype>(component.BoltType);
            args.PushMarkup(Loc.GetString("emitter-component-current-type", ("type", proto.Name)));
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

            _gun.Shoot(uid, gunComponent, ent, xform.Coordinates, targetPos, out _);
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
    }
}
