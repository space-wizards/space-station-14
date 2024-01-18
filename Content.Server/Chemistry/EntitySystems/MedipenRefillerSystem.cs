

using Content.Shared.Chemistry.Components;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using static Content.Shared.Chemistry.SharedMedipenRefiller;
using System.Linq;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class MedipenRefillerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedipenRefillerComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUserInterfaceState(u, c));
        SubscribeLocalEvent<MedipenRefillerComponent, MedipenRefillerSyncRequestMessage>(OnMedipenRefillerSyncRequestMessage);
    }
    public List<ProtoId<MedipenRecipePrototype>> GetRecipes(EntityUid uid, MedipenRefillerComponent component)
    {
        var ev = new MedipenRefillerGetRecipesEvent(uid)
        {
            Recipes = new List<ProtoId<MedipenRecipePrototype>>(component.StaticRecipes)
        };
        Console.WriteLine(ev.Recipes.Count.ToString());
        RaiseLocalEvent(uid, ev);
        return ev.Recipes;
    }

    #region UI Messages
    public void UpdateUserInterfaceState(EntityUid uid, MedipenRefillerComponent component)
    {
        if (!Resolve(uid, ref component!))
            return;

        var ui = _uiSys.GetUi(uid, MedipenRefillerUiKey.Key);

        var state = new MedipenRefillerUpdateState(GetRecipes(uid, component));
        _uiSys.SetUiState(ui, state);

        Console.WriteLine("I'm sending a user interface state to client.");
    }

    private void OnMedipenRefillerSyncRequestMessage(EntityUid uid, MedipenRefillerComponent component, MedipenRefillerSyncRequestMessage args)
    {
        UpdateUserInterfaceState(uid, component);

        Console.WriteLine("I'm receiving a request from client.");
    }
    #endregion
}
