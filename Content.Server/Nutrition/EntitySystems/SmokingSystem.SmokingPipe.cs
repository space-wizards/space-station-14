using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Temperature;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

        private void InitializePipes()
        {
            SubscribeLocalEvent<SmokingPipeComponent, InteractUsingEvent>(OnPipeInteractUsingEvent);
            SubscribeLocalEvent<SmokingPipeComponent, SmokableSolutionEmptyEvent>(OnPipeSolutionEmptyEvent);
            SubscribeLocalEvent<SmokingPipeComponent, AfterInteractEvent>(OnPipeAfterInteract);
            SubscribeLocalEvent<SmokingPipeComponent, ComponentInit>(OnComponentInit);
        }

        public void OnComponentInit(EntityUid uid, SmokingPipeComponent pipe, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, SmokingPipeComponent.BowlSlotId, pipe.BowlSlot);
        }

        private void OnPipeInteractUsingEvent(Entity<SmokingPipeComponent> pipe, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!EntityManager.TryGetComponent(pipe, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            if (TryTransferReagents((pipe, pipe, smokable)))
                SetSmokableState(pipe, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        public void OnPipeAfterInteract(Entity<SmokingPipeComponent> pipe, ref AfterInteractEvent args)
        {
            var targetEntity = args.Target;
            if (targetEntity == null ||
                !args.CanReach ||
                !EntityManager.TryGetComponent(pipe, out SmokableComponent? smokable) ||
                smokable.State == SmokableState.Lit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(targetEntity.Value, isHotEvent, true);

            if (!isHotEvent.IsHot)
                return;

            if(TryTransferReagents((pipe, pipe, smokable)))
                SetSmokableState(pipe, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        private void OnPipeSolutionEmptyEvent(Entity<SmokingPipeComponent> pipe, ref SmokableSolutionEmptyEvent args)
        {
            _itemSlotsSystem.SetLock(pipe, pipe.Comp.BowlSlot, false);
            SetSmokableState(pipe, SmokableState.Unlit);
        }

        // Convert smokable item into reagents to be smoked
        private bool TryTransferReagents(Entity<SmokingPipeComponent, SmokableComponent> pipe)
        {
            var (_, component, smokable) = pipe;
            if (component.BowlSlot.Item == null)
                return false;

            var contents = component.BowlSlot.Item.Value;

            if (!TryComp<SolutionContainerManagerComponent>(contents, out var reagents) ||
                !_solutionContainerSystem.TryGetSolution(pipe, smokable.Solution, out var pipeSolution))
                return false;

            foreach (var reagentSolution in reagents.Solutions)
            {
                _solutionContainerSystem.TryAddSolution(pipe, pipeSolution, reagentSolution.Value);
            }

            EntityManager.DeleteEntity(contents);

            _itemSlotsSystem.SetLock(pipe, component.BowlSlot, true); //no inserting more until current runs out

            return true;
        }
    }
}
