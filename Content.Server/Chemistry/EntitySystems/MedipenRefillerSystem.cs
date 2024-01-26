
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.EntityList;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class MedipenRefillerSystem : SharedMedipenRefillerSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlot = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedipenRefillerComponent, BeforeActivatableUIOpenEvent>((uid, c, _) => UpdateUserInterfaceState((uid, c)));
        SubscribeLocalEvent<MedipenRefillerComponent, EntInsertedIntoContainerMessage>((uid, c, _) => UpdateUserInterfaceState((uid, c)));
        SubscribeLocalEvent<MedipenRefillerComponent, EntRemovedFromContainerMessage>((uid, c, _) => UpdateUserInterfaceState((uid, c)));

        SubscribeLocalEvent<MedipenRefillerComponent, MedipenRefillerTransferReagentMessage>(OnTransferButtonMessage);
        SubscribeLocalEvent<MedipenRefillerComponent, MedipenRefillerActivateMessage>(OnActivateMessage);
    }

    private void OnTransferButtonMessage(Entity<MedipenRefillerComponent> entity, ref MedipenRefillerTransferReagentMessage message)
    {
        if (message.Amount <= 0)
            return;

        TransferReagent(entity, message.Id, message.Amount, message.IsBuffer);
    }

    private void OnActivateMessage(Entity<MedipenRefillerComponent> entity, ref MedipenRefillerActivateMessage message)
    {
        if (GetBufferSolution(entity) == null || !CanRefill(entity, GetBufferSolution(entity)!, message.MedipenRecipe.ID))
            return;

        var solution = GetBufferSolution(entity)!;
        entity.Comp.RemainingTime = entity.Comp.CompletionTime.Add(_timing.CurTime);
        entity.Comp.IsActivated = true;
        entity.Comp.Result = message.MedipenRecipe.ID;
        _itemSlot.SetLock(entity.Owner, entity.Comp.MedipenSlotName, true);
        solution.RemoveAllSolution();
        _audio.PlayPvs(entity.Comp.MachineNoise, entity.Owner);
        UpdateUserInterfaceState(entity);
    }

    private void FinishRefilling(EntityUid uid, MedipenRefillerComponent component)
    {
        Del(_itemSlot.GetItemOrNull(uid, component.MedipenSlotName));
        Spawn(component.Result, uid.ToCoordinates());
        _itemSlot.SetLock(uid, component.MedipenSlotName, false);
        component.IsActivated = false;
        component.RemainingTime = TimeSpan.Zero;
        component.Result = "";
    }

    private bool CanRefill(Entity<MedipenRefillerComponent> entity, Solution buffer, string id)
    {
        if (!_itemSlot.TryGetSlot(entity.Owner, entity.Comp.MedipenSlotName, out var slot))
            return false;

        return CanRefill(id, buffer.Contents, slot.HasItem);
    }

    private void TransferReagent(Entity<MedipenRefillerComponent> entity, ReagentId reagent, FixedPoint2 amount, bool isBuffer)
    {
        if (GetInputContainerSolution(entity) is null || GetBufferSolution(entity) is null || entity.Comp.IsActivated)
            return;

        var input = GetInputContainerSolution(entity)!.Value;
        var buffer = GetBufferSolution(entity);

        // Buffer -> Input, since this is buffer button message
        if (isBuffer)
        {
            var clampedAmount = FixedPoint2.Min(input.Item2!.MaxVolume - input.Item2.Volume, FixedPoint2.Min(buffer!.GetReagentQuantity(reagent), amount));
            buffer.RemoveReagent(reagent, clampedAmount, true);
            _solutionContainer.TryAddReagent(input.Item1!.Value, reagent, clampedAmount, out var _);
        }
        // Input -> Buffer, since this is input button message
        else
        {
            var clampedAmount = FixedPoint2.Min(buffer!.MaxVolume - buffer.Volume, FixedPoint2.Min(input.Item2!.GetReagentQuantity(reagent), amount));
            _solutionContainer.RemoveReagent(input.Item1!.Value, reagent, clampedAmount);
            buffer.AddReagent(reagent, clampedAmount);
        }

        UpdateUserInterfaceState(entity);
    }

    private (Entity<SolutionComponent>?, Solution?)? GetInputContainerSolution(Entity<MedipenRefillerComponent> entity)
    {
        if (!_itemSlot.TryGetSlot(entity, entity.Comp.InputSlotName, out var inputSlot) || !inputSlot.HasItem)
            return null;

        if (!_solutionContainer.TryGetFitsInDispenser(inputSlot.Item!.Value, out var soln, out var solution))
            return null;

        return (soln, solution);
    }

    private Solution? GetBufferSolution(Entity<MedipenRefillerComponent> entity)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.BufferSolutionName, out var _, out var solution))
            return null;

        return solution;
    }

    private ContainerData BuildInputContainerData(Entity<MedipenRefillerComponent> entity)
    {
        if (_itemSlot.TryGetSlot(entity, entity.Comp.InputSlotName, out var inputSlot)
            && inputSlot!.HasItem
            && _solutionContainer.TryGetFitsInDispenser(inputSlot.Item!.Value, out var _, out var solution))
            return new ContainerData(Name(inputSlot.Item.Value), solution.Contents, solution.Volume, solution.MaxVolume, true);

        return new ContainerData();
    }

    private ContainerData BuildBufferData(Entity<MedipenRefillerComponent> entity)
    {
        if (_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.BufferSolutionName, out var _, out var solution)
            && _itemSlot.TryGetSlot(entity, entity.Comp.MedipenSlotName, out var medipenSlot))
        {
            if (medipenSlot!.HasItem
                && _solutionContainer.TryGetSolution(medipenSlot.Item!.Value, entity.Comp.MedipenSolutionName, out var _, out var medipenSolution))
                return new ContainerData(Name(medipenSlot.Item!.Value), solution.Contents, solution.Volume, medipenSolution.MaxVolume, true);
            else if (solution.Volume > 0)
                return new ContainerData("buffer", solution.Contents, solution.Volume, 0, false);
        }

        return new ContainerData();
    }

    #region UI Messages
    public void UpdateUserInterfaceState(Entity<MedipenRefillerComponent> entity)
    {
        var resultName = "";
        var time = entity.Comp.RemainingTime.Subtract(_timing.CurTime);

        if (_prototypeManager.TryIndex<EntityPrototype>(entity.Comp.Result!, out var proto))
            resultName = proto.Name;

        var state = new MedipenRefillerUpdateState(BuildInputContainerData(entity), BuildBufferData(entity), entity.Comp.IsActivated, resultName, (int) time.TotalSeconds);

        _ui.TrySetUiState(entity, MedipenRefillerUiKey.Key, state);
    }
    #endregion

    /// <summary>
    /// Search for active components to tick it. If the time runs out, the entity will spawn.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MedipenRefillerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.IsActivated && component.RemainingTime <= _timing.CurTime)
            {
                FinishRefilling(uid, component);
            }
        }
    }
}
