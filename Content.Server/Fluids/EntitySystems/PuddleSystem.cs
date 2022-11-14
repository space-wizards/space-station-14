using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Solution = Content.Shared.Chemistry.Components.Solution;

namespace Content.Server.Fluids.EntitySystems
{
    [UsedImplicitly]
    public sealed class PuddleSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly FluidSpreaderSystem _fluidSpreaderSystem = default!;
        [Dependency] private readonly StepTriggerSystem _stepTrigger = default!;
        [Dependency] private readonly SlipperySystem _slipSystem = default!;
        [Dependency] private readonly EvaporationSystem _evaporationSystem = default!;


        public override void Initialize()
        {
            base.Initialize();

            // Shouldn't need re-anchoring.
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
            var volumeScale = CurrentVolume(puddleComponent.Owner, puddleComponent).Float() /
                              puddleComponent.OverflowVolume.Float() *
                              puddleComponent.OpacityModifier;
            var puddleSolution = _solutionContainerSystem.EnsureSolution(uid, puddleComponent.SolutionName);


            bool hasEvaporationComponent =
                EntityManager.TryGetComponent<EvaporationComponent>(uid, out var evaporationComponent);
            bool canEvaporate = (hasEvaporationComponent &&
                                 (evaporationComponent!.LowerLimit == 0 ||
                                  CurrentVolume(puddleComponent.Owner, puddleComponent) >
                                  evaporationComponent.LowerLimit));

            // "Does this puddle's sprite need changing to the wet floor effect sprite?"
            bool changeToWetFloor = (CurrentVolume(puddleComponent.Owner, puddleComponent) <=
                                     puddleComponent.WetFloorEffectThreshold
                                     && canEvaporate);

            appearanceComponent.SetData(PuddleVisuals.VolumeScale, volumeScale);
            appearanceComponent.SetData(PuddleVisuals.SolutionColor, puddleSolution.Color);
            appearanceComponent.SetData(PuddleVisuals.ForceWetFloorSprite, changeToWetFloor);
        }

        private void UpdateSlip(EntityUid entityUid, PuddleComponent puddleComponent)
        {
            if ((puddleComponent.SlipThreshold == FixedPoint2.New(-1) ||
                 CurrentVolume(puddleComponent.Owner, puddleComponent) < puddleComponent.SlipThreshold) &&
                TryComp(entityUid, out StepTriggerComponent? stepTrigger))
            {
                _stepTrigger.SetActive(entityUid, false, stepTrigger);
            }
            else if (CurrentVolume(puddleComponent.Owner, puddleComponent) >= puddleComponent.SlipThreshold)
            {
                var comp = EnsureComp<StepTriggerComponent>(entityUid);
                _stepTrigger.SetActive(entityUid, true, comp);
            }
        }

        private void HandlePuddleExamined(EntityUid uid, PuddleComponent component, ExaminedEvent args)
        {
            if (TryComp<StepTriggerComponent>(uid, out var slippery) && slippery.Active)
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
        /// Try to add solution to <paramref name="puddleUid"/>.
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
                    out var solution))
            {
                return false;
            }

            solution.AddSolution(addedSolution);
            if (checkForOverflow && IsOverflowing(puddleUid, puddleComponent))
            {
                _fluidSpreaderSystem.AddOverflowingPuddle(puddleComponent.Owner, puddleComponent);
            }

            RaiseLocalEvent(puddleComponent.Owner, new SolutionChangedEvent(), true);

            if (!sound)
            {
                return true;
            }

            SoundSystem.Play(puddleComponent.SpillSound.GetSound(),
                Filter.Pvs(puddleComponent.Owner), puddleComponent.Owner);
            return true;
        }

        /// <summary>
        /// Given a large srcPuddle and smaller destination puddles, this method will equalize their <see cref="Solution.CurrentVolume"/>
        /// </summary>
        /// <param name="srcPuddle">puddle that donates liquids to other puddles</param>
        /// <param name="destinationPuddles">List of puddles that we want to equalize, their puddle <see cref="Solution.CurrentVolume"/> should be less than sourcePuddleComponent</param>
        /// <param name="totalVolume">Total volume of src and destination puddle</param>
        /// <param name="stillOverflowing">optional parameter, that after equalization adds all still overflowing puddles.</param>
        /// <param name="sourcePuddleComponent">puddleComponent for <paramref name="srcPuddle"/></param>
        public void EqualizePuddles(EntityUid srcPuddle, List<PuddleComponent> destinationPuddles,
            FixedPoint2 totalVolume,
            HashSet<EntityUid>? stillOverflowing = null,
            PuddleComponent? sourcePuddleComponent = null)
        {
            if (!Resolve(srcPuddle, ref sourcePuddleComponent)
                || !_solutionContainerSystem.TryGetSolution(srcPuddle, sourcePuddleComponent.SolutionName,
                    out var srcSolution))
                return;

            var dividedVolume = totalVolume / (destinationPuddles.Count + 1);

            foreach (var destPuddle in destinationPuddles)
            {
                if (!_solutionContainerSystem.TryGetSolution(destPuddle.Owner, destPuddle.SolutionName,
                        out var destSolution))
                    continue;

                var takeAmount = FixedPoint2.Max(0, dividedVolume - destSolution.CurrentVolume);
                TryAddSolution(destPuddle.Owner, srcSolution.SplitSolution(takeAmount), false, false, destPuddle);
                if (stillOverflowing != null && IsOverflowing(destPuddle.Owner, destPuddle))
                {
                    stillOverflowing.Add(destPuddle.Owner);
                }
            }

            if (stillOverflowing != null && srcSolution.CurrentVolume > sourcePuddleComponent.OverflowVolume)
            {
                stillOverflowing.Add(srcPuddle);
            }
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

            return CurrentVolume(uid, puddle) + solution.TotalVolume > puddle.OverflowVolume;
        }

        /// <summary>
        ///     Whether adding this solution to this puddle would overflow.
        /// </summary>
        /// <param name="uid">Uid of owning entity</param>
        /// <param name="puddle">Puddle ref param</param>
        /// <returns></returns>
        private bool IsOverflowing(EntityUid uid, PuddleComponent? puddle = null)
        {
            if (!Resolve(uid, ref puddle))
                return false;

            return CurrentVolume(uid, puddle) > puddle.OverflowVolume;
        }

        public PuddleComponent SpawnPuddle(EntityUid srcUid, EntityCoordinates pos, PuddleComponent? srcPuddleComponent = null)
        {
            MetaDataComponent? metadata = null;
            Resolve(srcUid, ref srcPuddleComponent, ref metadata);

            var prototype = metadata?.EntityPrototype?.ID ?? "PuddleSmear"; // TODO Spawn a entity based on another entity

            var destUid = EntityManager.SpawnEntity(prototype, pos);
            var destPuddle = EntityManager.EnsureComponent<PuddleComponent>(destUid);

            return destPuddle;
        }
    }
}
