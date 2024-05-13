using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Flash.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Radio;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Trigger;
using Content.Shared.Weapons.Ranged.Events;
using JetBrains.Annotations;
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
    /// Raised when timer trigger becomes active.
    /// </summary>
    [ByRefEvent]
    public readonly record struct ActiveTimerTriggerEvent(EntityUid Triggered, EntityUid? User);

    [UsedImplicitly]
    public sealed partial class TriggerSystem : SharedTriggerSystem
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
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;

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
            SubscribeLocalEvent<RattleComponent, TriggerEvent>(HandleRattleTrigger);
        }

        private void OnSoundTrigger(EntityUid uid, SoundOnTriggerComponent component, ref TriggerEvent args)
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

        private void OnAnchorTrigger(EntityUid uid, AnchorOnTriggerComponent component, ref TriggerEvent args)
        {
            var xform = Transform(uid);

            if (xform.Anchored)
                return;

            _transformSystem.AnchorEntity(uid, xform);

            if (component.RemoveOnTrigger)
                RemCompDeferred<AnchorOnTriggerComponent>(uid);
        }

        private void OnSpawnTrigger(EntityUid uid, SpawnOnTriggerComponent component, ref TriggerEvent args)
        {
            var xform = Transform(uid);

            var coords = xform.Coordinates;

            if (!coords.IsValid(EntityManager))
                return;

            Spawn(component.Proto, coords);
        }

        private void HandleExplodeTrigger(EntityUid uid, ExplodeOnTriggerComponent component, ref TriggerEvent args)
        {
            _explosions.TriggerExplosive(uid, user: args.User);
            args.Handled = true;
        }

        private void HandleFlashTrigger(EntityUid uid, FlashOnTriggerComponent component, ref TriggerEvent args)
        {
            // TODO Make flash durations sane ffs.
            _flashSystem.FlashArea(uid, args.User, component.Range, component.Duration * 1000f, probability: component.Probability);
            args.Handled = true;
        }

        private void HandleDeleteTrigger(EntityUid uid, DeleteOnTriggerComponent component, ref TriggerEvent args)
        {
            EntityManager.QueueDeleteEntity(uid);
            args.Handled = true;
        }

        private void HandleGibTrigger(EntityUid uid, GibOnTriggerComponent component, ref TriggerEvent args)
        {
            if (!TryComp<TransformComponent>(uid, out var xform))
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

        private void HandleRattleTrigger(EntityUid uid, RattleComponent component, ref TriggerEvent args)
        {
            if (!TryComp<SubdermalImplantComponent>(uid, out var implanted))
                return;

            if (implanted.ImplantedEntity == null)
                return;

            // Gets location of the implant
            var posText = FormattedMessage.RemoveMarkup(_navMap.GetNearestBeaconString(uid));
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
                Trigger(uid);
        }

        private void OnSpawnTriggered(EntityUid uid, TriggerOnSpawnComponent component, MapInitEvent args)
        {
            Trigger(uid);
        }

        private void OnActivate(EntityUid uid, TriggerOnActivateComponent component, ActivateInWorldEvent args)
        {
            Trigger(uid, args.User);
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
