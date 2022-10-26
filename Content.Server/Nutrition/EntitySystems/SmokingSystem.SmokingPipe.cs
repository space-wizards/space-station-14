using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Nutrition.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.PDA;
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

        private void OnPipeInteractUsingEvent(EntityUid uid, SmokingPipeComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!EntityManager.TryGetComponent(uid, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            if (TryTransferReagents(component, smokable))
                SetSmokableState(uid, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        public void OnPipeAfterInteract(EntityUid uid, SmokingPipeComponent component, AfterInteractEvent args)
        {
            var targetEntity = args.Target;
            if (targetEntity == null ||
                !args.CanReach ||
                !EntityManager.TryGetComponent(uid, out SmokableComponent? smokable) ||
                smokable.State == SmokableState.Lit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(targetEntity.Value, isHotEvent, true);

            if (!isHotEvent.IsHot)
                return;

            if(TryTransferReagents(component, smokable))
                SetSmokableState(uid, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        private void OnPipeSolutionEmptyEvent(EntityUid uid, SmokingPipeComponent component, SmokableSolutionEmptyEvent args)
        {
            _itemSlotsSystem.SetLock(component.Owner, component.BowlSlot, false);
            SetSmokableState(uid, SmokableState.Unlit);
        }

        // Convert smokable item into reagents to be smoked
        private bool TryTransferReagents(SmokingPipeComponent component, SmokableComponent smokable)
        {
            if (component.BowlSlot.Item == null)
                return false;

            EntityUid contents = component.BowlSlot.Item.Value;

            if (!TryComp<SolutionContainerManagerComponent>(contents, out var reagents) ||
                !_solutionContainerSystem.TryGetSolution(smokable.Owner, smokable.Solution, out var pipeSolution))
                return false;

            foreach (var reagentSolution in reagents.Solutions)
            {
                _solutionContainerSystem.TryAddSolution(smokable.Owner, pipeSolution, reagentSolution.Value);
            }

            EntityManager.DeleteEntity(contents);

            _itemSlotsSystem.SetLock(component.Owner, component.BowlSlot, true); //no inserting more until current runs out

            return true;
        }
    }
}
