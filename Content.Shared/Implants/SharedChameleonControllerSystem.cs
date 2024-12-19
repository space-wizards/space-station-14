namespace Content.Shared.Implants;

public sealed class SharedChameleonControllerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChameleonControllerOpenMenuEvent>(OpenUI);
    }

    private void OpenUI(ChameleonControllerOpenMenuEvent ev)
    {
        var implant = ev.Action.Comp.Container;

        if (!HasComp<ChameleonControllerImplantComponent>(implant))
            return;

        if (!_uiSystem.HasUi(implant.Value, ChameleonControllerKey.Key))
            return;

        _uiSystem.OpenUi(implant.Value, ChameleonControllerKey.Key, ev.Performer);
        _uiSystem.SetUiState(implant.Value, ChameleonControllerKey.Key, new ChameleonControllerBuiState());
    }
}
