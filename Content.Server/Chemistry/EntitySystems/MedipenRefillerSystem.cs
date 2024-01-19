
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
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
        SubscribeLocalEvent<MedipenRefillerComponent, MedipenRefillerSyncRequestMessage>(OnMedipenRefillerSyncRequestMessage);
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

    private void OnComponentStartup(EntityUid uid, MedipenRefillerComponent component, ComponentStartup args)
    {
        if (!Resolve(uid, ref component!))
            return;

        UpdateRecipes(uid, component);
    }

    private void OnComponentStartup(EntityUid uid, EmaggedComponent component, ComponentStartup args)
    {
        if (!Resolve(uid, ref component!) || !_entityManager.TryGetComponent<MedipenRefillerComponent>(uid, out var refillerComponent))
            return;

        UpdateRecipes(uid, refillerComponent);
    }

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

    #region UI Messages
    public void UpdateUserInterfaceState(EntityUid uid, MedipenRefillerComponent component)
    {
        if (!Resolve(uid, ref component!))
            return;

        var ui = _uiSys.GetUi(uid, SharedMedipenRefiller.MedipenRefillerUiKey.Key);

        var state = new MedipenRefillerUpdateState(component.MedipenRecipes);
        _uiSys.SetUiState(ui, state);
    }

    private void OnMedipenRefillerSyncRequestMessage(EntityUid uid, MedipenRefillerComponent component, MedipenRefillerSyncRequestMessage args)
    {
        UpdateUserInterfaceState(uid, component);
    }
    #endregion
}
