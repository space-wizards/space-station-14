using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Shared.Database;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Payload.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Trigger;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

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

        public override void Initialize()
        {
            base.Initialize();

            InitializeProximity();
            InitializeOnUse();
            InitializeSignal();
            InitializeTimedCollide();
            InitializeVoice();
            InitializeMobstate();

            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnTriggerCollide);
            SubscribeLocalEvent<TriggerOnActivateComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<TriggerImplantActionComponent, ActivateImplantEvent>(OnImplantTrigger);
            SubscribeLocalEvent<TriggerOnStepTriggerComponent, StepTriggeredEvent>(OnStepTriggered);

            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(HandleExplodeTrigger);
            SubscribeLocalEvent<FlashOnTriggerComponent, TriggerEvent>(HandleFlashTrigger);
            SubscribeLocalEvent<GibOnTriggerComponent, TriggerEvent>(HandleGibTrigger);
        }

        private void HandleExplodeTrigger(EntityUid uid, ExplodeOnTriggerComponent component, TriggerEvent args)
        {
            _explosions.TriggerExplosive(uid, user: args.User);
            args.Handled = true;
        }

        #region Flash
        private void HandleFlashTrigger(EntityUid uid, FlashOnTriggerComponent component, TriggerEvent args)
        {
            // TODO Make flash durations sane ffs.
            _flashSystem.FlashArea(uid, args.User, component.Range, component.Duration * 1000f);
            args.Handled = true;
        }
        #endregion

        private void HandleDeleteTrigger(EntityUid uid, DeleteOnTriggerComponent component, TriggerEvent args)
        {
            EntityManager.QueueDeleteEntity(uid);
            args.Handled = true;
        }

        private void HandleGibTrigger(EntityUid uid, GibOnTriggerComponent component, TriggerEvent args)
        {
            if (!TryComp<TransformComponent>(uid, out var xform))
                return;

            _body.GibBody(xform.ParentUid, deleteItems: component.DeleteItems);

            args.Handled = true;
        }


        private void OnTriggerCollide(EntityUid uid, TriggerOnCollideComponent component, ref StartCollideEvent args)
        {
			if(args.OurFixture.ID == component.FixtureID && (!component.IgnoreOtherNonHard || args.OtherFixture.Hard))
				Trigger(component.Owner);
        }

        private void OnActivate(EntityUid uid, TriggerOnActivateComponent component, ActivateInWorldEvent args)
        {
            Trigger(component.Owner, args.User);
            args.Handled = true;
        }

        private void OnImplantTrigger(EntityUid uid, TriggerImplantActionComponent component, ActivateImplantEvent args)
        {
            Trigger(uid);
        }

        private void OnStepTriggered(EntityUid uid, TriggerOnStepTriggerComponent component, ref StepTriggeredEvent args)
        {
            Trigger(uid, args.Tripper);
        }

        public bool Trigger(EntityUid trigger, EntityUid? user = null)
        {
            var triggerEvent = new TriggerEvent(trigger, user);
            EntityManager.EventBus.RaiseLocalEvent(trigger, triggerEvent, true);
            return triggerEvent.Handled;
        }

        public void HandleTimerTrigger(EntityUid uid, EntityUid? user, float delay , float beepInterval, float? initialBeepDelay, SoundSpecifier? beepSound, AudioParams beepParams)
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
                if (_container.TryGetContainer(uid, "payload", out IContainer? container) &&
                    container.ContainedEntities.Count > 0 &&
                    TryComp(container.ContainedEntities[0], out ChemicalPayloadComponent? chemicalPayloadComponent))
                {
                    // If a beaker is missing, the entity won't explode, so no reason to log it
                    if (!TryComp(chemicalPayloadComponent?.BeakerSlotA.Item, out SolutionContainerManagerComponent? beakerA) ||
                        !TryComp(chemicalPayloadComponent?.BeakerSlotB.Item, out SolutionContainerManagerComponent? beakerB))
                        return;

                    _adminLogger.Add(LogType.Trigger,
                        $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}, which contains [{string.Join(", ", beakerA.Solutions.Values.First())}] in one beaker and [{string.Join(", ", beakerB.Solutions.Values.First())}] in the other.");
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
            active.BeepParams = beepParams;
            active.BeepSound = beepSound;
            active.BeepInterval = beepInterval;
            active.TimeUntilBeep = initialBeepDelay == null ? active.BeepInterval : initialBeepDelay.Value;

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearance.SetData(uid, TriggerVisuals.VisualState, TriggerVisualState.Primed, appearance);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProximity(frameTime);
            UpdateTimer(frameTime);
            UpdateTimedCollide(frameTime);
        }

        private void UpdateTimer(float frameTime)
        {
            HashSet<EntityUid> toRemove = new();
            foreach (var timer in EntityQuery<ActiveTimerTriggerComponent>())
            {
                timer.TimeRemaining -= frameTime;
                timer.TimeUntilBeep -= frameTime;

                if (timer.TimeRemaining <= 0)
                {
                    Trigger(timer.Owner, timer.User);
                    toRemove.Add(timer.Owner);
                    continue;
                }

                if (timer.BeepSound == null || timer.TimeUntilBeep > 0)
                    continue;

                timer.TimeUntilBeep += timer.BeepInterval;
                var filter = Filter.Pvs(timer.Owner, entityManager: EntityManager);
                SoundSystem.Play(timer.BeepSound.GetSound(), filter, timer.Owner, timer.BeepParams);
            }

            foreach (var uid in toRemove)
            {
                RemComp<ActiveTimerTriggerComponent>(uid);

                // In case this is a re-usable grenade, un-prime it.
                if (TryComp<AppearanceComponent>(uid, out var appearance))
                    _appearance.SetData(uid, TriggerVisuals.VisualState, TriggerVisualState.Unprimed, appearance);
            }
        }
    }
}
