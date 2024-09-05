using Robust.Client.UserInterface;

namespace Content.Client.Xenobiology.UI;

public sealed class CellularFusionBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CellularFusionWindow? _window;

    public CellularFusionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CellularFusionWindow>();
    }
}
