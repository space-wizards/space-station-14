using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Content.Shared.Slippery;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.Server.Fluids
{
    [UsedImplicitly]
    internal sealed class PuddleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            _mapManager.TileChanged += HandleTileChanged;

            SubscribeLocalEvent<SpillableComponent, GetOtherVerbsEvent>(AddSpillVerb);
            SubscribeLocalEvent<PuddleComponent, ExaminedEvent>(HandlePuddleExamined);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.TileChanged -= HandleTileChanged;
        }

        private void AddSpillVerb(EntityUid uid, SpillableComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!_solutionContainerSystem.TryGetDrainableSolution(args.Target.Uid, out var solution))
                return;

            if (solution.DrainAvailable == ReagentUnit.Zero)
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("spill-target-verb-get-data-text");
            // TODO VERB ICONS spill icon? pouring out a glass/beaker?
            verb.Act = () => _solutionContainerSystem.SplitSolution(args.Target.Uid,
                solution, solution.DrainAvailable).SpillAt(args.Target.Transform.Coordinates, "PuddleSmear");
            args.Verbs.Add(verb);
        }

        private void HandlePuddleExamined(EntityUid uid, PuddleComponent component, ExaminedEvent args)
        {
            if (EntityManager.TryGetComponent<SlipperyComponent>(uid, out var slippery) && slippery.Slippery)
            {
                args.PushText(Loc.GetString("puddle-component-examine-is-slipper-text"));
            }
        }

        //TODO: Replace all this with an Unanchored event that deletes the puddle
        private void HandleTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // If this gets hammered you could probably queue up all the tile changes every tick but I doubt that would ever happen.
            foreach (var puddle in EntityManager.EntityQuery<PuddleComponent>(true))
            {
                // If the tile becomes space then delete it (potentially change by design)
                var puddleTransform = puddle.Owner.Transform;
                if(!puddleTransform.Anchored)
                    continue;

                var grid = _mapManager.GetGrid(puddleTransform.GridID);
                if (eventArgs.NewTile.GridIndex == puddle.Owner.Transform.GridID &&
                    grid.TileIndicesFor(puddleTransform.Coordinates) == eventArgs.NewTile.GridIndices &&
                    eventArgs.NewTile.Tile.IsEmpty)
                {
                    puddle.Owner.QueueDelete();
                    break; // Currently it's one puddle per tile, if that changes remove this
                }
            }
        }
    }
}
