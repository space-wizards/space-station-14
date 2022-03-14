using Content.Server.Administration.Logs;
using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Timer = Robust.Shared.Timing.Timer;
using Content.Shared.Payload.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Database;

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
        [Dependency] private readonly DoorSystem _sharedDoorSystem = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeProximity();
            InitializeOnUse();

            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnTriggerCollide);

            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(HandleExplodeTrigger);
            SubscribeLocalEvent<FlashOnTriggerComponent, TriggerEvent>(HandleFlashTrigger);
            SubscribeLocalEvent<ToggleDoorOnTriggerComponent, TriggerEvent>(HandleDoorTrigger);
            SubscribeLocalEvent<ChemicalPayloadComponent, TriggerEvent>(HandleChemichalPayloadTrigger);
        }

        private void HandleChemichalPayloadTrigger(EntityUid uid, ChemicalPayloadComponent component, TriggerEvent args)
        {
            if (component.BeakerSlotA.Item is not EntityUid beakerA)
                return;

            if (component.BeakerSlotB.Item is not EntityUid beakerB)
                return;

            if (!TryComp(beakerA, out FitsInDispenserComponent? compA))
                return;

            if (!TryComp(beakerB, out FitsInDispenserComponent? compB))
                return;

            if (!_solutionSystem.TryGetSolution(beakerA, compA.Solution, out var solutionA))
                return;

            if (!_solutionSystem.TryGetSolution(beakerB, compB.Solution, out var solutionB))
                return;

            if (solutionA.TotalVolume == 0 || solutionB.TotalVolume == 0)
                return;

            var solStringA = SolutionContainerSystem.ToPrettyString(solutionA);
            var solStringB = SolutionContainerSystem.ToPrettyString(solutionB);

            _logSystem.Add(LogType.ChemicalReaction,
                $"Chemical bomb payload {ToPrettyString(uid):payload} at {Transform(uid).MapPosition:location} is combining two solutions: {solStringA:solutionA} and {solStringB:solutionB}");

            // entity will be deleted anyway, just modify the max volume instead of creating a new solution.
            solutionA.MaxVolume = Shared.FixedPoint.FixedPoint2.MaxValue;
            solutionA.CanReact = true;
            _solutionSystem.TryAddSolution(component.BeakerSlotA.Item!.Value, solutionA, solutionB);
        }

        #region Explosions
        private void HandleExplodeTrigger(EntityUid uid, ExplodeOnTriggerComponent component, TriggerEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out ExplosiveComponent? explosiveComponent)) return;

            Explode(uid, explosiveComponent, args.User);
        }

        // You really shouldn't call this directly (TODO Change that when ExplosionHelper gets changed).
        public void Explode(EntityUid uid, ExplosiveComponent component, EntityUid? user = null)
        {
            if (component.Exploding)
            {
                return;
            }

            component.Exploding = true;
            _explosions.SpawnExplosion(uid,
                component.DevastationRange,
                component.HeavyImpactRange,
                component.LightImpactRange,
                component.FlashRange,
                user);
            EntityManager.QueueDeleteEntity(uid);
        }
        #endregion

        #region Flash
        private void HandleFlashTrigger(EntityUid uid, FlashOnTriggerComponent component, TriggerEvent args)
        {
            // TODO Make flash durations sane ffs.
            _flashSystem.FlashArea(uid, args.User, component.Range, component.Duration * 1000f);
        }
        #endregion

        private void HandleDeleteTrigger(EntityUid uid, DeleteOnTriggerComponent component, TriggerEvent args)
        {
            EntityManager.QueueDeleteEntity(uid);
        }

        private void HandleDoorTrigger(EntityUid uid, ToggleDoorOnTriggerComponent component, TriggerEvent args)
        {
            _sharedDoorSystem.TryToggleDoor(uid);
        }

        private void OnTriggerCollide(EntityUid uid, TriggerOnCollideComponent component, StartCollideEvent args)
        {
            Trigger(component.Owner);
        }


        public void Trigger(EntityUid trigger, EntityUid? user = null)
        {
            var triggerEvent = new TriggerEvent(trigger, user);
            EntityManager.EventBus.RaiseLocalEvent(trigger, triggerEvent);
        }

        public void HandleTimerTrigger(TimeSpan delay, EntityUid triggered, EntityUid? user = null)
        {
            if (delay.TotalSeconds <= 0)
            {
                Trigger(triggered, user);
                return;
            }

            Timer.Spawn(delay, () =>
            {
                if (Deleted(triggered)) return;
                Trigger(triggered, user);
            });
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProximity(frameTime);
        }
    }
}
