using Content.Server.Construction.Components;
using Content.Server.Tools;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;

namespace Content.Server.Construction
{
    public sealed class RefiningSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WelderRefinableComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private async void OnInteractUsing(EntityUid uid, WelderRefinableComponent component, InteractUsingEvent args)
        {
            // check if object is welder
            if (!HasComp<ToolComponent>(args.Used))
                return;

            // check if someone is already welding object
            if (component.BeingWelded)
                return;

            component.BeingWelded = true;

            if (!await _toolSystem.UseTool(args.Used, args.User, uid, component.RefineFuel, component.RefineTime, component.QualityNeeded))
            {
                // failed to veld - abort refine
                component.BeingWelded = false;
                return;
            }

            // get last owner coordinates and delete it
            var resultPosition = Transform(uid).Coordinates;
            EntityManager.DeleteEntity(uid);

            // spawn each result after refine
            foreach (var result in component.RefineResult!)
            {
                var droppedEnt = EntityManager.SpawnEntity(result, resultPosition);

                // TODO: If something has a stack... Just use a prototype with a single thing in the stack.
                // This is not a good way to do it.
                if (TryComp<StackComponent?>(droppedEnt, out var stack))
                    _stackSystem.SetCount(droppedEnt,1, stack);
            }
        }
    }
}
