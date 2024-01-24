
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class MedipenRefillerSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedipenRefillerComponent, BeforeActivatableUIOpenEvent>((uid, c, _) => UpdateUserInterfaceState(new Entity<MedipenRefillerComponent>(uid, c)));
        SubscribeLocalEvent<MedipenRefillerComponent, EntInsertedIntoContainerMessage>((uid, c, _) => UpdateUserInterfaceState(new Entity<MedipenRefillerComponent>(uid, c)));
        SubscribeLocalEvent<MedipenRefillerComponent, EntRemovedFromContainerMessage>((uid, c, _) => UpdateUserInterfaceState(new Entity<MedipenRefillerComponent>(uid, c)));

        SubscribeLocalEvent<MedipenRefillerComponent, MedipenRefillerTransferReagentMessage>(OnTransferButtonMessage);
        SubscribeLocalEvent<MedipenRefillerComponent, MedipenRefillerActivateMessage>(OnActivateMessage);

        SubscribeLocalEvent<MedipenRefillerComponent, ComponentStartup>(OnComponentStartup);

        SubscribeLocalEvent<EmaggedComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<MedipenRefillerComponent, GotEmaggedEvent>(OnGotEmmaged);
    }

    private void OnGotEmmaged(Entity<MedipenRefillerComponent> entity, ref GotEmaggedEvent args)
    {
        if (_entityManager.HasComponent<EmaggedComponent>(entity.Owner))
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Inserts data from refillable medipens as soon as the entity is initialized.
    /// </summary>
    private void OnComponentStartup(Entity<MedipenRefillerComponent> entity, ref ComponentStartup args)
    {
        UpdateRecipes(entity);
    }

    /// <summary>
    /// Make sure that the recipes are updated when the entity is emagged, as it wouldn't be desirable to update every time the interface is opened by the client.
    /// </summary>
    private void OnComponentStartup(EntityUid uid, EmaggedComponent _, ComponentStartup __)
    {
        if (!_entityManager.TryGetComponent<MedipenRefillerComponent>(uid, out var component))
            return;

        UpdateRecipes(new Entity<MedipenRefillerComponent>(uid, component));
    }

    private void OnTransferButtonMessage(Entity<MedipenRefillerComponent> entity, ref MedipenRefillerTransferReagentMessage message)
    {
        if (message.Amount <= 0)
            return;

        TransferReagent(entity, message.Id, message.Amount, message.IsBuffer);
    }

    private void OnActivateMessage(Entity<MedipenRefillerComponent> entity, ref MedipenRefillerActivateMessage message)
    {
        if (GetBufferSolution(entity.Owner) == null || !CanRefill(entity, GetBufferSolution(entity.Owner)!, message.MedipenRecipe.ID))
            return;

        var solution = GetBufferSolution(entity.Owner)!;
        entity.Comp.RemainingTime = entity.Comp.CompletionTime;
        entity.Comp.IsActivated = true;
        entity.Comp.Result = message.MedipenRecipe.Result!;
        _itemSlotsSystem.SetLock(entity.Owner, SharedMedipenRefiller.MedipenSlotName, true);
        solution.RemoveAllSolution();
        _audioSystem.PlayPvs(entity.Comp.MachineNoise, entity.Owner);
        UpdateUserInterfaceState(entity);
    }

    private bool CanRefill(Entity<MedipenRefillerComponent> entity, Solution buffer, string id)
    {
        if (!_itemSlotsSystem.TryGetSlot(entity.Owner, SharedMedipenRefiller.MedipenSlotName, out var slot))
            return false;

        return SharedMedipenRefiller.CanRefill(id, entity.Comp.MedipenRecipes, buffer.Contents, _prototypeManager, slot.HasItem);
    }

    /// <summary>
    /// Deserializes the recipe prototype for medipens. If new medipen recipes are added, ensure that their ID is in the string list of the component.
    /// </summary>
    private void UpdateRecipes(Entity<MedipenRefillerComponent> entity)
    {
        var recipeList = new List<MedipenRecipePrototype>();

        foreach (var medipen in entity.Comp.MedipenList)
        {
            if (!_prototypeManager.HasIndex<MedipenRecipePrototype>(medipen))
                continue;

            var recipe = _prototypeManager.Index<MedipenRecipePrototype>(medipen);
            if (!recipe.LockedByEmag || _entityManager.HasComponent<EmaggedComponent>(entity.Owner))
                recipeList.Add(recipe);
        }

        entity.Comp.MedipenRecipes = recipeList;
        UpdateUserInterfaceState(entity);
    }

    private void TransferReagent(Entity<MedipenRefillerComponent> entity, ReagentId reagent, FixedPoint2 amount, bool isBuffer)
    {
        var input = GetInputContainerSolution(entity.Owner);
        var buffer = GetBufferSolution(entity.Owner);

        if (input is null || buffer is null || entity.Comp.IsActivated)
            return;

        // Buffer -> Input, since this is buffer button message
        if (isBuffer)
        {
            var clampedAmount = FixedPoint2.Min(input.Item2.MaxVolume - input.Item2.Volume, FixedPoint2.Min(buffer.GetReagentQuantity(reagent), amount));
            buffer.RemoveReagent(reagent, clampedAmount, true);
            _solutionContainerSystem.TryAddReagent(input.Item1!.Value, reagent, clampedAmount, out var _);
        }
        // Input -> Buffer, since this is input button message
        else
        {
            var clampedAmount = FixedPoint2.Min(buffer.MaxVolume - buffer.Volume, FixedPoint2.Min(input.Item2.GetReagentQuantity(reagent), amount));
            _solutionContainerSystem.RemoveReagent(input.Item1!.Value, reagent, clampedAmount);
            buffer.AddReagent(reagent, clampedAmount);
        }

        UpdateUserInterfaceState(entity);
    }

    private Tuple<Entity<SolutionComponent>?, Solution>? GetInputContainerSolution(EntityUid uid)
    {
        if (!_itemSlotsSystem.TryGetSlot(uid, SharedMedipenRefiller.InputSlotName, out var inputSlot) || !inputSlot.HasItem)
            return null;

        if (!_solutionContainerSystem.TryGetFitsInDispenser(inputSlot.Item!.Value, out var soln, out var solution))
            return null;

        return Tuple.Create(soln, solution);
    }

    private Solution? GetBufferSolution(EntityUid uid)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, SharedMedipenRefiller.BufferSolutionName, out var _, out var solution))
            return null;

        return solution;
    }

    private ContainerData BuildInputContainerData(EntityUid uid)
    {
        if (_itemSlotsSystem.TryGetSlot(uid, SharedMedipenRefiller.InputSlotName, out var inputSlot)
            && inputSlot!.HasItem
            && _solutionContainerSystem.TryGetFitsInDispenser(inputSlot.Item!.Value, out var _, out var solution))
            return new ContainerData(Name(inputSlot.Item.Value), solution.Contents, solution.Volume, solution.MaxVolume, true);

        return new ContainerData();
    }

    private ContainerData BuildBufferData(EntityUid uid)
    {
        if (_solutionContainerSystem.TryGetSolution(uid, SharedMedipenRefiller.BufferSolutionName, out var _, out var solution)
            && _itemSlotsSystem.TryGetSlot(uid, SharedMedipenRefiller.MedipenSlotName, out var medipenSlot))
        {
            if (medipenSlot.HasItem
                && _solutionContainerSystem.TryGetSolution(medipenSlot.Item!.Value, SharedMedipenRefiller.MedipenSolutionName, out var _, out var medipenSolution))
                return new ContainerData(Name(medipenSlot.Item!.Value), solution.Contents, solution.Volume, medipenSolution.MaxVolume, true);
            else if (solution.Volume > 0)
                return new ContainerData("buffer", solution.Contents, solution.Volume, 0, false);
        }

        return new ContainerData();
    }

    #region UI Messages
    public void UpdateUserInterfaceState(Entity<MedipenRefillerComponent> entity)
    {
        var ui = _uiSys.GetUi(entity, SharedMedipenRefiller.MedipenRefillerUiKey.Key);

        string resultName = "";

        if (_prototypeManager.TryIndex<EntityPrototype>(entity.Comp.Result, out var proto))
            resultName = proto.Name;

        var state = new MedipenRefillerUpdateState(entity.Comp.MedipenRecipes, BuildInputContainerData(entity.Owner), BuildBufferData(entity.Owner),
                                                   entity.Comp.IsActivated, resultName, (int) entity.Comp.RemainingTime);

        _uiSys.SetUiState(ui, state);
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
            if (component.IsActivated)
            {
                component.RemainingTime -= frameTime;
                if (component.RemainingTime <= 0 && _prototypeManager.HasIndex<EntityPrototype>(component.Result))
                {
                    _entityManager.DeleteEntity(_itemSlotsSystem.GetItemOrNull(uid, SharedMedipenRefiller.MedipenSlotName));
                    _itemSlotsSystem.SetLock(uid, SharedMedipenRefiller.MedipenSlotName, false);
                    _entityManager.SpawnEntity(component.Result, uid.ToCoordinates());
                    component.IsActivated = false;
                    component.RemainingTime = 0;
                    component.Result = "";
                }
            }
        }
    }
}
