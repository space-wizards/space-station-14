using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Electrocution;
using Content.Server.Pinpointer;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Flash.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Database;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Payload.Components;
using Content.Shared.Radio;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Trigger;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Explosion.EntitySystems
{
    /// <summary>
    /// Raised whenever something is Triggered on the entity.
    /// </summary>
    public sealed class TriggerEvent : HandledEntityEventArgs
    {
        public EntityUid Triggered { get; }
        public EntityUid? User { get; }

        public TriggerEvent(EntityUid triggered, EntityUid? user = null)
        {
            Triggered = triggered;
            User = user;
        }
    }

    /// <summary>
    /// Raised before a trigger is activated.
    /// </summary>
    [ByRefEvent]
    public record struct BeforeTriggerEvent(EntityUid Triggered, EntityUid? User, bool Cancelled = false);

    /// <summary>
    /// Raised when timer trigger becomes active.
    /// </summary>
    [ByRefEvent]
    public readonly record struct ActiveTimerTriggerEvent(EntityUid Triggered, EntityUid? User);

    [UsedImplicitly]
    public sealed partial class TriggerSystem : EntitySystem
    {
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly FlashSystem _flashSystem = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly NavMapSystem _navMap = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
        [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeProximity();
            InitializeOnUse();
            InitializeSignal();
            InitializeTimedCollide();
            InitializeVoice();
            InitializeMobstate();

            SubscribeLocalEvent<TriggerOnSpawnComponent, MapInitEvent>(OnSpawnTriggered);
            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnTriggerCollide);
            SubscribeLocalEvent<TriggerOnActivateComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<TriggerOnUseComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<TriggerImplantActionComponent, ActivateImplantEvent>(OnImplantTrigger);
            SubscribeLocalEvent<TriggerOnStepTriggerComponent, StepTriggeredOffEvent>(OnStepTriggered);
            SubscribeLocalEvent<TriggerOnSlipComponent, SlipEvent>(OnSlipTriggered);
            SubscribeLocalEvent<TriggerWhenEmptyComponent, OnEmptyGunShotEvent>(OnEmptyTriggered);
            SubscribeLocalEvent<RepeatingTriggerComponent, MapInitEvent>(OnRepeatInit);

            SubscribeLocalEvent<SpawnOnTriggerComponent, TriggerEvent>(OnSpawnTrigger);
            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(HandleExplodeTrigger);
            SubscribeLocalEvent<FlashOnTriggerComponent, TriggerEvent>(HandleFlashTrigger);
            SubscribeLocalEvent<GibOnTriggerComponent, TriggerEvent>(HandleGibTrigger);

            SubscribeLocalEvent<AnchorOnTriggerComponent, TriggerEvent>(OnAnchorTrigger);
            SubscribeLocalEvent<SoundOnTriggerComponent, TriggerEvent>(OnSoundTrigger);
            SubscribeLocalEvent<ShockOnTriggerComponent, TriggerEvent>(HandleShockTrigger);
            SubscribeLocalEvent<RattleComponent, TriggerEvent>(HandleRattleTrigger);

            SubscribeLocalEvent<TriggerWhitelistComponent, BeforeTriggerEvent>(HandleWhitelist);
        }

        private void HandleWhitelist(Entity<TriggerWhitelistComponent> ent, ref BeforeTriggerEvent args)
        {
            args.Cancelled = !_whitelist.CheckBoth(args.User, ent.Comp.Blacklist, ent.Comp.Whitelist);
        }

        private void OnSoundTrigger(EntityUid uid, SoundOnTriggerComponent component, TriggerEvent args)
        {
            if (component.RemoveOnTrigger) // if the component gets removed when it's triggered
            {
                var xform = Transform(uid);
                _audio.PlayPvs(component.Sound, xform.Coordinates); // play the sound at its last known coordinates
            }
            else // if the component doesn't get removed when triggered
            {
                _audio.PlayPvs(component.Sound, uid); // have the sound follow the entity itself
            }
        }

        private void HandleShockTrigger(Entity<ShockOnTriggerComponent> shockOnTrigger, ref TriggerEvent args)
        {
            if (!_container.TryGetContainingContainer(shockOnTrigger.Owner, out var container))
                return;

            var containerEnt = container.Owner;
            var curTime = _timing.CurTime;

            if (curTime < shockOnTrigger.Comp.NextTrigger)
            {
                // The trigger's on cooldown.
                return;
            }

            _electrocution.TryDoElectrocution(containerEnt, null, shockOnTrigger.Comp.Damage, shockOnTrigger.Comp.Duration, true);
            shockOnTrigger.Comp.NextTrigger = curTime + shockOnTrigger.Comp.Cooldown;
        }

        private void OnAnchorTrigger(EntityUid uid, AnchorOnTriggerComponent component, TriggerEvent args)
        {
            var xform = Transform(uid);

            if (xform.Anchored)
                return;

            _transformSystem.AnchorEntity(uid, xform);

            if (component.RemoveOnTrigger)
                RemCompDeferred<AnchorOnTriggerComponent>(uid);
        }

        private void OnSpawnTrigger(Entity<SpawnOnTriggerComponent> ent, ref TriggerEvent args)
        {
            var xform = Transform(ent);

            if (ent.Comp.mapCoords)
            {
                var mapCoords = _transformSystem.GetMapCoordinates(ent, xform);
                Spawn(ent.Comp.Proto, mapCoords);
            }
            else
            {
                var coords = xform.Coordinates;
                if (!coords.IsValid(EntityManager))
                    return;
                Spawn(ent.Comp.Proto, coords);

            }
        }

        private void HandleExplodeTrigger(EntityUid uid, ExplodeOnTriggerComponent component, TriggerEvent args)
        {
            _explosions.TriggerExplosive(uid, user: args.User);
            args.Handled = true;
        }

        private void HandleFlashTrigger(EntityUid uid, FlashOnTriggerComponent component, TriggerEvent args)
        {
            // TODO Make flash durations sane ffs.
            _flashSystem.FlashArea(uid, args.User, component.Range, component.Duration * 1000f, probability: component.Probability);
            args.Handled = true;
        }

        private void HandleDeleteTrigger(EntityUid uid, DeleteOnTriggerComponent component, TriggerEvent args)
        {
            EntityManager.QueueDeleteEntity(uid);
            args.Handled = true;
        }

        private void HandleGibTrigger(EntityUid uid, GibOnTriggerComponent component, TriggerEvent args)
        {
            if (!TryComp(uid, out TransformComponent? xform))
                return;
            if (component.DeleteItems)
            {
                var items = _inventory.GetHandOrInventoryEntities(xform.ParentUid);
                foreach (var item in items)
                {
                    Del(item);
                }
            }
            _body.GibBody(xform.ParentUid, true);
            args.Handled = true;
        }


        private void HandleRattleTrigger(EntityUid uid, RattleComponent component, TriggerEvent args)
        {
            if (!TryComp<SubdermalImplantComponent>(uid, out var implanted))
                return;

            if (implanted.ImplantedEntity == null)
                return;

            // Gets location of the implant
            var posText = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(uid));
            var critMessage = Loc.GetString(component.CritMessage, ("user", implanted.ImplantedEntity.Value), ("position", posText));
            var deathMessage = Loc.GetString(component.DeathMessage, ("user", implanted.ImplantedEntity.Value), ("position", posText));

            if (!TryComp<MobStateComponent>(implanted.ImplantedEntity, out var mobstate))
                return;

            // Sends a message to the radio channel specified by the implant
            if (mobstate.CurrentState == MobState.Critical)
                _radioSystem.SendRadioMessage(uid, critMessage, _prototypeManager.Index<RadioChannelPrototype>(component.RadioChannel), uid);
            if (mobstate.CurrentState == MobState.Dead)
                _radioSystem.SendRadioMessage(uid, deathMessage, _prototypeManager.Index<RadioChannelPrototype>(component.RadioChannel), uid);

            args.Handled = true;
        }

        private void OnTriggerCollide(EntityUid uid, TriggerOnCollideComponent component, ref StartCollideEvent args)
        {
            if (args.OurFixtureId == component.FixtureID && (!component.IgnoreOtherNonHard || args.OtherFixture.Hard))
                Trigger(uid, args.OtherEntity);
        }

        private void OnSpawnTriggered(EntityUid uid, TriggerOnSpawnComponent component, MapInitEvent args)
        {
            Trigger(uid);
        }

        private void OnActivate(EntityUid uid, TriggerOnActivateComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            Trigger(uid, args.User);
            args.Handled = true;
        }

        private void OnUse(Entity<TriggerOnUseComponent> ent, ref UseInHandEvent args)
        {
            if (args.Handled)
                return;

            Trigger(ent.Owner, args.User);
            args.Handled = true;
        }

        private void OnImplantTrigger(EntityUid uid, TriggerImplantActionComponent component, ActivateImplantEvent args)
        {
            args.Handled = Trigger(uid);
        }

        private void OnStepTriggered(EntityUid uid, TriggerOnStepTriggerComponent component, ref StepTriggeredOffEvent args)
        {
            Trigger(uid, args.Tripper);
        }

        private void OnSlipTriggered(EntityUid uid, TriggerOnSlipComponent component, ref SlipEvent args)
        {
            Trigger(uid, args.Slipped);
        }

        private void OnEmptyTriggered(EntityUid uid, TriggerWhenEmptyComponent component, ref OnEmptyGunShotEvent args)
        {
            Trigger(uid, args.EmptyGun);
        }

        private void OnRepeatInit(Entity<RepeatingTriggerComponent> ent, ref MapInitEvent args)
        {
            ent.Comp.NextTrigger = _timing.CurTime + ent.Comp.Delay;
        }

        public bool Trigger(EntityUid trigger, EntityUid? user = null)
        {
            var beforeTriggerEvent = new BeforeTriggerEvent(trigger, user);
            RaiseLocalEvent(trigger, ref beforeTriggerEvent);
            if (beforeTriggerEvent.Cancelled)
                return false;

            var triggerEvent = new TriggerEvent(trigger, user);
            EntityManager.EventBus.RaiseLocalEvent(trigger, triggerEvent, true);
            return triggerEvent.Handled;
        }

        public void TryDelay(EntityUid uid, float amount, ActiveTimerTriggerComponent? comp = null)
        {
            if (!Resolve(uid, ref comp, false))
                return;

            comp.TimeRemaining += amount;
        }

        /// <summary>
        /// Start the timer for triggering the device.
        /// </summary>
        public void StartTimer(Entity<OnUseTimerTriggerComponent?> ent, EntityUid? user)
        {
            if (!Resolve(ent, ref ent.Comp, false))
                return;

            var comp = ent.Comp;
            HandleTimerTrigger(ent, user, comp.Delay, comp.BeepInterval, comp.InitialBeepDelay, comp.BeepSound);
        }

        public void HandleTimerTrigger(EntityUid uid, EntityUid? user, float delay, float beepInterval, float? initialBeepDelay, SoundSpecifier? beepSound)
        {
            if (delay <= 0)
            {
                RemComp<ActiveTimerTriggerComponent>(uid);
                Trigger(uid, user);
                return;
            }

            if (HasComp<ActiveTimerTriggerComponent>(uid))
                return;

            if (user != null)
            {
                // Check if entity is bomb/mod. grenade/etc
                if (_container.TryGetContainer(uid, "payload", out BaseContainer? container) &&
                    container.ContainedEntities.Count > 0 &&
                    TryComp(container.ContainedEntities[0], out ChemicalPayloadComponent? chemicalPayloadComponent))
                {
                    // If a beaker is missing, the entity won't explode, so no reason to log it
                    if (chemicalPayloadComponent?.BeakerSlotA.Item is not { } beakerA ||
                        chemicalPayloadComponent?.BeakerSlotB.Item is not { } beakerB ||
                        !TryComp(beakerA, out SolutionContainerManagerComponent? containerA) ||
                        !TryComp(beakerB, out SolutionContainerManagerComponent? containerB) ||
                        !TryComp(beakerA, out FitsInDispenserComponent? fitsA) ||
                        !TryComp(beakerB, out FitsInDispenserComponent? fitsB) ||
                        !_solutionContainerSystem.TryGetSolution((beakerA, containerA), fitsA.Solution, out _, out var solutionA) ||
                        !_solutionContainerSystem.TryGetSolution((beakerB, containerB), fitsB.Solution, out _, out var solutionB))
                        return;

                    _adminLogger.Add(LogType.Trigger,
                        $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}, which contains {SharedSolutionContainerSystem.ToPrettyString(solutionA)} in one beaker and {SharedSolutionContainerSystem.ToPrettyString(solutionB)} in the other.");
                }
                else
                {
                    _adminLogger.Add(LogType.Trigger,
                        $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}");
                }

            }
            else
            {
                _adminLogger.Add(LogType.Trigger,
                    $"{delay} second timer trigger started on entity {ToPrettyString(uid):timer}");
            }

            var active = AddComp<ActiveTimerTriggerComponent>(uid);
            active.TimeRemaining = delay;
            active.User = user;
            active.BeepSound = beepSound;
            active.BeepInterval = beepInterval;
            active.TimeUntilBeep = initialBeepDelay == null ? active.BeepInterval : initialBeepDelay.Value;

            var ev = new ActiveTimerTriggerEvent(uid, user);
            RaiseLocalEvent(uid, ref ev);

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearance.SetData(uid, TriggerVisuals.VisualState, TriggerVisualState.Primed, appearance);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProximity();
            UpdateTimer(frameTime);
            UpdateTimedCollide(frameTime);
            UpdateRepeat();
        }

        private void UpdateTimer(float frameTime)
        {
            HashSet<EntityUid> toRemove = new();
            var query = EntityQueryEnumerator<ActiveTimerTriggerComponent>();
            while (query.MoveNext(out var uid, out var timer))
            {
                timer.TimeRemaining -= frameTime;
                timer.TimeUntilBeep -= frameTime;

                if (timer.TimeRemaining <= 0)
                {
                    Trigger(uid, timer.User);
                    toRemove.Add(uid);
                    continue;
                }

                if (timer.BeepSound == null || timer.TimeUntilBeep > 0)
                    continue;

                timer.TimeUntilBeep += timer.BeepInterval;
                _audio.PlayPvs(timer.BeepSound, uid, timer.BeepSound.Params);
            }

            foreach (var uid in toRemove)
            {
                RemComp<ActiveTimerTriggerComponent>(uid);

                // In case this is a re-usable grenade, un-prime it.
                if (TryComp<AppearanceComponent>(uid, out var appearance))
                    _appearance.SetData(uid, TriggerVisuals.VisualState, TriggerVisualState.Unprimed, appearance);
            }
        }

        private void UpdateRepeat()
        {
            var now = _timing.CurTime;
            var query = EntityQueryEnumerator<RepeatingTriggerComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.NextTrigger > now)
                    continue;

                comp.NextTrigger = now + comp.Delay;
                Trigger(uid);
            }
        }
    }
}
