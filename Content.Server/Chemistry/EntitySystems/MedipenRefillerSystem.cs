

using Content.Server.Chemistry.Components;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class MedipenRefillerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedipenRefillerComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUserInterfaceState(u, c));
        SubscribeLocalEvent<MedipenRefillerComponent, MedipenRefillerSyncRequestMessage>(OnMedipenRefillerSyncRequestMessage);
        SubscribeLocalEvent<MedipenRefillerComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(EntityUid uid, MedipenRefillerComponent component, ComponentStartup args)
    {
        if (!Resolve(uid, ref component!))
            return;

        var recipeList = new List<MedipenRecipePrototype>();

        foreach (var medipen in component.MedipenList)
        {
            if (!_prototypeManager.HasIndex<MedipenRecipePrototype>(medipen))
                continue;

            recipeList.Add(_prototypeManager.Index<MedipenRecipePrototype>(medipen));
        }

        component.MedipenRecipes = recipeList;
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
