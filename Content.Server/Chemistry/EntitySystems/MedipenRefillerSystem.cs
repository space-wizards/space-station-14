
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Tag;
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


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedipenRefillerComponent, BeforeActivatableUIOpenEvent>((uid, c, _) => UpdateUserInterfaceState(uid, c));
        SubscribeLocalEvent<MedipenRefillerComponent, EntInsertedIntoContainerMessage>((uid, c, _) => UpdateUserInterfaceState(uid, c));
        SubscribeLocalEvent<MedipenRefillerComponent, EntRemovedFromContainerMessage>((uid, c, _) => UpdateUserInterfaceState(uid, c));
        SubscribeLocalEvent<MedipenRefillerComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<EmaggedComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<MedipenRefillerComponent, GotEmaggedEvent>(OnGotEmmaged);
    }

    private void OnGotEmmaged(EntityUid uid, MedipenRefillerComponent component, ref GotEmaggedEvent args)
    {
        if (!Resolve(uid, ref component!) || _entityManager.HasComponent<EmaggedComponent>(uid))
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Inserts data from fabricable medipens as soon as the entity is initialized.
    /// </summary>
    private void OnComponentStartup(EntityUid uid, MedipenRefillerComponent component, ComponentStartup args)
    {
        if (!Resolve(uid, ref component!))
            return;

        UpdateRecipes(uid, component);
    }

    /// <summary>
    /// Make sure that the recipes are updated when the entity is emagged, as it wouldn't be desirable to update every time the interface is opened by the client.
    /// </summary>
    private void OnComponentStartup(EntityUid uid, EmaggedComponent component, ComponentStartup args)
    {
        if (!Resolve(uid, ref component!) || !_entityManager.TryGetComponent<MedipenRefillerComponent>(uid, out var refillerComponent))
            return;

        UpdateRecipes(uid, refillerComponent);
    }

    /// <summary>
    /// Deserializes the recipe prototype for medipens. If new medipen recipes are added, ensure that their ID is in the string list of the component.
    /// </summary>
    private void UpdateRecipes(EntityUid uid, MedipenRefillerComponent component)
    {
        var recipeList = new List<MedipenRecipePrototype>();

        foreach (var medipen in component.MedipenList)
        {
            if (!_prototypeManager.HasIndex<MedipenRecipePrototype>(medipen))
                continue;

            var recipe = _prototypeManager.Index<MedipenRecipePrototype>(medipen);
            if (!recipe.LockedByEmag || _entityManager.HasComponent<EmaggedComponent>(uid))
                recipeList.Add(recipe);
        }

        component.MedipenRecipes = recipeList;
        UpdateUserInterfaceState(uid, component);
    }

    /// <summary>
    /// If it's positive, transfer from the container to the buffer. If it's negative, transfer from the buffer to the container.
    /// </summary>
    private void TransferReagent(EntityUid uid, MedipenRefillerComponent component, ReagentId reagent, FixedPoint2 amount)
    {
        var input = GetInputContainerSolution(uid, component);
        var buffer = GetBufferSolution(uid, component);

        if (input == null || buffer == null)
            return;

        if (amount > 0 && input.GetReagentQuantity(reagent) <= amount)
        {
            input.RemoveReagent(reagent, amount, true);
            buffer!.AddReagent(reagent, amount);
        }
        else if (amount < 0 && buffer!.GetReagentQuantity(reagent) <= FixedPoint2.Abs(amount))
        {
            buffer!.RemoveReagent(reagent, FixedPoint2.Abs(amount), true);
            input.AddReagent(reagent, FixedPoint2.Abs(amount));
        }

        UpdateUserInterfaceState(uid, component);
    }

    private ItemSlot? GetInputSlot(EntityUid uid, MedipenRefillerComponent component)
    {
        if (!Resolve(uid, ref component!)
            || !_itemSlotsSystem.TryGetSlot(uid, SharedMedipenRefiller.InputSlotName, out var inputSlot))
            return null;

        return inputSlot;
    }

    private ItemSlot? GetMedipenSlot(EntityUid uid, MedipenRefillerComponent component)
    {
        if (!Resolve(uid, ref component!)
            || !_itemSlotsSystem.TryGetSlot(uid, SharedMedipenRefiller.MedipenSlotName, out var medipenSlot))
            return null;

        return medipenSlot;
    }

    private Solution? GetInputContainerSolution(EntityUid uid, MedipenRefillerComponent component)
    {
        if (!Resolve(uid, ref component!)
            || !_itemSlotsSystem.TryGetSlot(uid, SharedMedipenRefiller.InputSlotName, out var inputSlot)
            || !inputSlot.HasItem)
            return null;

        if (!_solutionContainerSystem.TryGetFitsInDispenser(inputSlot.Item!.Value, out var _, out var solution))
            return null;

        return solution;
    }

    private Solution? GetBufferSolution(EntityUid uid, MedipenRefillerComponent component)
    {
        if (!Resolve(uid, ref component!)
            || !_solutionContainerSystem.TryGetSolution(uid, SharedMedipenRefiller.BufferSolutionName, out var _, out var solution))
            return null;

        return solution;
    }

    private ContainerData BuildInputContainerData(EntityUid uid)
    {
        if (_itemSlotsSystem.TryGetSlot(uid, SharedMedipenRefiller.InputSlotName, out var inputSlot) && inputSlot!.HasItem
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
    public void UpdateUserInterfaceState(EntityUid uid, MedipenRefillerComponent component)
    {
        if (!Resolve(uid, ref component!))
            return;

        var ui = _uiSys.GetUi(uid, SharedMedipenRefiller.MedipenRefillerUiKey.Key);

        var state = new MedipenRefillerUpdateState(component.MedipenRecipes, BuildInputContainerData(uid), BuildBufferData(uid));

        _uiSys.SetUiState(ui, state);
    }
    #endregion
}
