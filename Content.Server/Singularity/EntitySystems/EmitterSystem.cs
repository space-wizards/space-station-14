using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Projectiles;
using Content.Server.Storage.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
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
            SubscribeLocalEvent<EmitterComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<EmitterComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<EmitterComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        }

        private void OnAnchorStateChanged(EntityUid uid, EmitterComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                return;

            SwitchOff(component);
        }

        private void OnInteractHand(EntityUid uid, EmitterComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            if (EntityManager.TryGetComponent(uid, out LockComponent? lockComp) && lockComp.Locked)
            {
                _popup.PopupEntity(Loc.GetString("comp-emitter-access-locked",
                    ("target", component.Owner)), uid, args.User);
                return;
            }

            if (EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
            {
                if (!component.IsOn)
                {
                    SwitchOn(component);
                    _popup.PopupEntity(Loc.GetString("comp-emitter-turned-on",
                        ("target", component.Owner)), uid, args.User);
                }
                else
                {
                    SwitchOff(component);
                    _popup.PopupEntity(Loc.GetString("comp-emitter-turned-off",
                        ("target", component.Owner)), uid, args.User);
                }

                _adminLogger.Add(LogType.Emitter,
                    component.IsOn ? LogImpact.Medium : LogImpact.High,
                    $"{ToPrettyString(args.User):player} toggled {ToPrettyString(uid):emitter}");
                args.Handled = true;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("comp-emitter-not-anchored",
                    ("target", component.Owner)), uid, args.User);
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
                PowerOff(component);
            }
            else
            {
                PowerOn(component);
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
                PowerOff(component);
            }
            else
            {
                PowerOn(component);
            }
        }

        private void OnRefreshParts(EntityUid uid, EmitterComponent component, RefreshPartsEvent args)
        {
            var fireRateRating = args.PartRatings[component.MachinePartFireRate];

            component.FireInterval = component.BaseFireInterval * MathF.Pow(component.FireRateMultiplier, fireRateRating - 1);
            component.FireBurstDelayMin = component.BaseFireBurstDelayMin * MathF.Pow(component.FireRateMultiplier, fireRateRating - 1);
            component.FireBurstDelayMax = component.BaseFireBurstDelayMax * MathF.Pow(component.FireRateMultiplier, fireRateRating - 1);
        }

        private void OnUpgradeExamine(EntityUid uid, EmitterComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("emitter-component-upgrade-fire-rate", (float) (component.BaseFireInterval.TotalSeconds / component.FireInterval.TotalSeconds));
        }

        public void SwitchOff(EmitterComponent component)
        {
            component.IsOn = false;
            if (TryComp<PowerConsumerComponent>(component.Owner, out var powerConsumer))
                powerConsumer.DrawRate = 1; // this needs to be not 0 so that the visuals still work.
            if (TryComp<ApcPowerReceiverComponent>(component.Owner, out var apcReceiever))
                apcReceiever.Load = 1;
            PowerOff(component);
            UpdateAppearance(component);
        }

        public void SwitchOn(EmitterComponent component)
        {
            component.IsOn = true;
            if (TryComp<PowerConsumerComponent>(component.Owner, out var powerConsumer))
                powerConsumer.DrawRate = component.PowerUseActive;
            if (TryComp<ApcPowerReceiverComponent>(component.Owner, out var apcReceiever))
            {
                apcReceiever.Load = component.PowerUseActive;
                PowerOn(component);
            }
            // Do not directly PowerOn().
            // OnReceivedPowerChanged will get fired due to DrawRate change which will turn it on.
            UpdateAppearance(component);
        }

        public void PowerOff(EmitterComponent component)
        {
            if (!component.IsPowered)
            {
                return;
            }

            component.IsPowered = false;

            // Must be set while emitter powered.
            DebugTools.AssertNotNull(component.TimerCancel);
            component.TimerCancel?.Cancel();

            UpdateAppearance(component);
        }

        public void PowerOn(EmitterComponent component)
        {
            if (component.IsPowered)
            {
                return;
            }

            component.IsPowered = true;

            component.FireShotCounter = 0;
            component.TimerCancel = new CancellationTokenSource();

            Timer.Spawn(component.FireBurstDelayMax, () => ShotTimerCallback(component), component.TimerCancel.Token);

            UpdateAppearance(component);
        }

        private void ShotTimerCallback(EmitterComponent component)
        {
            if (component.Deleted)
                return;

            // Any power-off condition should result in the timer for this method being cancelled
            // and thus not firing
            DebugTools.Assert(component.IsPowered);
            DebugTools.Assert(component.IsOn);

            Fire(component);

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
            Timer.Spawn(delay, () => ShotTimerCallback(component), component.TimerCancel!.Token);
        }

        private void Fire(EmitterComponent component)
        {
            var uid = component.Owner;
            if (!TryComp<GunComponent>(uid, out var guncomp))
                return;

            var xform = Transform(uid);
            var ent = Spawn(component.BoltType, xform.Coordinates);
            var proj = EnsureComp<ProjectileComponent>(ent);
            _projectile.SetShooter(proj, uid);

            var targetPos = new EntityCoordinates(uid, (0, -1));
            _gun.Shoot(uid, guncomp, ent, xform.Coordinates, targetPos);
        }

        private void UpdateAppearance(EmitterComponent component)
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
            _appearance.SetData(component.Owner, EmitterVisuals.VisualState, state);
        }
    }
}
