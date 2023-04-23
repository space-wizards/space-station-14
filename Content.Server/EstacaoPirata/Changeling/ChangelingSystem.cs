using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Changeling;
using Content.Shared.Interaction;
using Robust.Shared.Player;
using Content.Server.Changeling.Shop;
using Content.Server.Humanoid;
using Content.Server.Changeling;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Actions;

public sealed class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly ChangelingShopSystem _changShopSystem = default!;
    //[Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ActionsSystem _action = default!;



    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnStartup); // teste

        SubscribeLocalEvent<ChangelingComponent, InstantActionEvent>(OnActionPeformed); // pra quando usar acao

        SubscribeLocalEvent<ChangelingComponent, ChangelingShopActionEvent>(OnShop); // pra abrir o shop
    }
    private void OnShop(EntityUid uid, ChangelingComponent component, ChangelingShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    private void OnStartup(EntityUid uid, ChangelingComponent component, ComponentStartup args)
    {
        //update the icon
        //ChangeEssenceAmount(uid, 0, component);

        var shopaction = new InstantAction(_proto.Index<InstantActionPrototype>("ChangelingShop"));
        _action.AddAction(uid, shopaction, null);
    }

    private void OnActionPeformed(EntityUid uid, ChangelingComponent component, InstantActionEvent args)
    {

    }
}