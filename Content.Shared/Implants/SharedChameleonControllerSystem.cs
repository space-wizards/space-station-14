using Content.Shared.Clothing.EntitySystems;

namespace Content.Shared.Implants;

public abstract partial class SharedChameleonControllerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonControllerOpenMenuEvent>(OpenUI);
        SubscribeLocalEvent<ChameleonControllerImplantComponent, ImplantRelayEvent<CanAccessChameleonClothingEvent>>(OnCanAccessRelay);
    }

    private void OpenUI(ChameleonControllerOpenMenuEvent ev)
    {
        var implant = ev.Action.Comp.Container;

        if (!HasComp<ChameleonControllerImplantComponent>(implant))
            return;

        if (!_uiSystem.HasUi(implant.Value, ChameleonControllerKey.Key))
            return;

        _uiSystem.OpenUi(implant.Value, ChameleonControllerKey.Key, ev.Performer);
    }

    private void OnCanAccessRelay(Entity<ChameleonControllerImplantComponent> ent, ref ImplantRelayEvent<CanAccessChameleonClothingEvent> args)
    {
        args.Event.CanAccess = true;
    }
}
