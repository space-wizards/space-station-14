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

        public void OnComponentInit(Entity<SmokingPipeComponent> entity, ref ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(entity, SmokingPipeComponent.BowlSlotId, entity.Comp.BowlSlot);
        }

        private void OnPipeInteractUsingEvent(Entity<SmokingPipeComponent> entity, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!EntityManager.TryGetComponent(entity, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            if (TryTransferReagents(entity.Comp, smokable))
                SetSmokableState(entity, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        public void OnPipeAfterInteract(Entity<SmokingPipeComponent> entity, ref AfterInteractEvent args)
        {
            var targetEntity = args.Target;
            if (targetEntity == null ||
                !args.CanReach ||
                !EntityManager.TryGetComponent(entity, out SmokableComponent? smokable) ||
                smokable.State == SmokableState.Lit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(targetEntity.Value, isHotEvent, true);

            if (!isHotEvent.IsHot)
                return;

            if (TryTransferReagents(entity.Comp, smokable))
                SetSmokableState(entity, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        private void OnPipeSolutionEmptyEvent(Entity<SmokingPipeComponent> entity, ref SmokableSolutionEmptyEvent args)
        {
            _itemSlotsSystem.SetLock(entity, entity.Comp.BowlSlot, false);
            SetSmokableState(entity, SmokableState.Unlit);
        }

        // Convert smokable item into reagents to be smoked
        private bool TryTransferReagents(SmokingPipeComponent component, SmokableComponent smokable)
        {
            if (component.BowlSlot.Item == null)
                return false;

            EntityUid contents = component.BowlSlot.Item.Value;

            if (!TryComp<SolutionContainerManagerComponent>(contents, out var reagents) ||
                !_solutionContainerSystem.TryGetSolution(smokable.Owner, smokable.Solution, out var pipeSolution, out _))
                return false;

            foreach (var (_, soln) in _solutionContainerSystem.EnumerateSolutions((contents, reagents)))
            {
                var reagentSolution = soln.Comp.Solution;
                _solutionContainerSystem.TryAddSolution(pipeSolution.Value, reagentSolution);
            }

            EntityManager.DeleteEntity(contents);

            _itemSlotsSystem.SetLock(component.Owner, component.BowlSlot, true); //no inserting more until current runs out

            return true;
        }
    }
}
