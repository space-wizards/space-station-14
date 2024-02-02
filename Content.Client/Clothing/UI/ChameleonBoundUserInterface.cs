using Content.Client.Clothing.Systems;
using Content.Shared.Clothing.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.UI;

[UsedImplicitly]
public sealed class ChameleonBoundUserInterface : BoundUserInterface
{
    private readonly ChameleonClothingSystem _chameleon;

    [ViewVariables]
    private ChameleonMenu? _menu;

    public ChameleonBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _chameleon = EntMan.System<ChameleonClothingSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = new ChameleonMenu();
        _menu.OnClose += Close;
        _menu.OnIdSelected += OnIdSelected;
        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ChameleonBoundUserInterfaceState st)
            return;

        var targets = _chameleon.GetValidTargets(st.Slot, st.Whitelist);
        _menu?.UpdateState(targets, st.SelectedId);
    }

    private void OnIdSelected(EntProtoId selectedId)
    {
        SendMessage(new ChameleonPrototypeSelectedMessage(selectedId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _menu?.Close();
            _menu = null;
        }
    }
}
