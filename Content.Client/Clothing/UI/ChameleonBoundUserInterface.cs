using Content.Client.Clothing.Systems;
using Content.Shared.Clothing.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

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

        _menu = this.CreateWindow<ChameleonMenu>();
        _menu.OnIdSelected += OnIdSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ChameleonBoundUserInterfaceState st)
            return;

        var targets = _chameleon.GetValidTargets(st.Slot);
        _menu?.UpdateState(targets, st.SelectedId);
    }

    private void OnIdSelected(string selectedId)
    {
        SendMessage(new ChameleonPrototypeSelectedMessage(selectedId));
    }
}
