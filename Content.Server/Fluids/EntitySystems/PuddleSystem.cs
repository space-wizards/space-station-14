using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Slippery;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Fluids.EntitySystems
{
    [UsedImplicitly]
    public sealed class PuddleSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly FluidSpreaderSystem _fluidSpreaderSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PuddleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<PuddleComponent, ExaminedEvent>(HandlePuddleExamined);
            SubscribeLocalEvent<PuddleComponent, SolutionChangedEvent>(OnUpdate);
            SubscribeLocalEvent<PuddleComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, PuddleComponent component, ComponentInit args)
        {
            var solution = _solutionContainerSystem.EnsureSolution(uid, component.SolutionName);
            solution.MaxVolume = FixedPoint2.New(1000);
        }

        private void OnUpdate(EntityUid uid, PuddleComponent component, SolutionChangedEvent args)
        {
            UpdateSlip(uid, component);
            UpdateVisuals(uid, component);
        }

        private void UpdateVisuals(EntityUid uid, PuddleComponent puddleComponent)
        {
            if (Deleted(puddleComponent.Owner) || EmptyHolder(uid, puddleComponent) ||
                !EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearanceComponent))
            {
                return;
            }

            // Opacity based on level of fullness to overflow
            // Hard-cap lower bound for visibility reasons
            var volumeScale = puddleComponent.CurrentVolume.Float() / puddleComponent.OverflowVolume.Float();
            var puddleSolution = _solutionContainerSystem.EnsureSolution(uid, puddleComponent.SolutionName);


            bool hasEvaporationComponent = EntityManager.TryGetComponent<EvaporationComponent>(uid, out var evaporationComponent);
            bool canEvaporate = (hasEvaporationComponent &&
                                (evaporationComponent.LowerLimit == 0 || puddleComponent.CurrentVolume > evaporationComponent.LowerLimit));

            // "Does this puddle's sprite need changing to the wet floor effect sprite?"
            bool changeToWetFloor = (puddleComponent.CurrentVolume <= puddleComponent.WetFloorEffectThreshold
                                    && canEvaporate);

            appearanceComponent.SetData(PuddleVisuals.VolumeScale, volumeScale);
            appearanceComponent.SetData(PuddleVisuals.SolutionColor, puddleSolution.Color);
            appearanceComponent.SetData(PuddleVisuals.ForceWetFloorSprite, changeToWetFloor);
        }

        private void UpdateSlip(EntityUid entityUid, PuddleComponent puddleComponent)
        {
            if ((puddleComponent.SlipThreshold == FixedPoint2.New(-1) ||
                 puddleComponent.CurrentVolume < puddleComponent.SlipThreshold) &&
                EntityManager.TryGetComponent(entityUid, out SlipperyComponent? oldSlippery))
            {
                oldSlippery.Slippery = false;
            }
            else if (puddleComponent.CurrentVolume >= puddleComponent.SlipThreshold)
            {
                var newSlippery = EntityManager.EnsureComponent<SlipperyComponent>(entityUid);
                newSlippery.Slippery = true;
            }
        }

        private void HandlePuddleExamined(EntityUid uid, PuddleComponent component, ExaminedEvent args)
        {
            if (EntityManager.TryGetComponent<SlipperyComponent>(uid, out var slippery) && slippery.Slippery)
            {
                args.PushText(Loc.GetString("puddle-component-examine-is-slipper-text"));
            }
        }

        private void OnAnchorChanged(EntityUid uid, PuddleComponent puddle, ref AnchorStateChangedEvent args)
        {
            if (!args.Anchored)
                QueueDel(uid);
        }

        public bool EmptyHolder(EntityUid uid, PuddleComponent? puddleComponent = null)
        {
            if (!Resolve(uid, ref puddleComponent))
                return true;

            return !_solutionContainerSystem.TryGetSolution(puddleComponent.Owner, puddleComponent.SolutionName,
                       out var solution)
                   || solution.Contents.Count == 0;
        }

        public FixedPoint2 CurrentVolume(EntityUid uid, PuddleComponent? puddleComponent = null)
        {
            if (!Resolve(uid, ref puddleComponent))
                return FixedPoint2.Zero;

            return _solutionContainerSystem.TryGetSolution(puddleComponent.Owner, puddleComponent.SolutionName,
                out var solution)
                ? solution.CurrentVolume
                : FixedPoint2.Zero;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="puddleUid">Puddle to which we add</param>
        /// <param name="addedSolution">Solution that is added to puddleComponent</param>
        /// <param name="sound">Play sound on overflow</param>
        /// <param name="checkForOverflow">Overflow on encountered values</param>
        /// <param name="puddleComponent">Optional resolved PuddleComponent</param>
        /// <returns></returns>
        public bool TryAddSolution(EntityUid puddleUid,
            Solution addedSolution,
            bool sound = true,
            bool checkForOverflow = true,
            PuddleComponent? puddleComponent = null)
        {
            if (!Resolve(puddleUid, ref puddleComponent))
                return false;

            if (addedSolution.TotalVolume == 0 ||
                !_solutionContainerSystem.TryGetSolution(puddleComponent.Owner, puddleComponent.SolutionName,
                    out var puddleSolution))
            {
                return false;
            }

            var result = _solutionContainerSystem
                .TryMixAndOverflow(puddleComponent.Owner, puddleSolution, addedSolution, puddleComponent.OverflowVolume,
                    out var overflowSolution);

            if (checkForOverflow && overflowSolution != null)
            {
                _fluidSpreaderSystem.AddOverflowingPuddle(puddleComponent, overflowSolution);
            }

            if (!result)
            {
                return false;
            }

            RaiseLocalEvent(puddleComponent.Owner, new SolutionChangedEvent());

            if (!sound)
            {
                return true;
            }

            SoundSystem.Play(Filter.Pvs(puddleComponent.Owner), puddleComponent.SpillSound.GetSound(),
                puddleComponent.Owner);
            return true;
        }

        /// <summary>
        ///     Whether adding this solution to this puddle would overflow.
        /// </summary>
        /// <param name="uid">Uid of owning entity</param>
        /// <param name="puddle">Puddle to which we are adding solution</param>
        /// <param name="solution">Solution we intend to add</param>
        /// <returns></returns>
        public bool WouldOverflow(EntityUid uid, Solution solution, PuddleComponent? puddle = null)
        {
            if (!Resolve(uid, ref puddle))
                return false;

            return puddle.CurrentVolume + solution.TotalVolume > puddle.OverflowVolume;
        }
    }
}
