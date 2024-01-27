using Content.Shared.Pinpointer;
using JetBrains.Annotations;

namespace Content.Client.Pinpointer.UI;

[UsedImplicitly]
public sealed class NavMapBeaconBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NavMapBeaconWindow? _window;

    public NavMapBeaconBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new NavMapBeaconWindow(Owner);
        _window.OpenCentered();
        _window.OnClose += Close;

        _window.OnApplyButtonPressed += (label, enabled, color) =>
        {
            SendMessage(new NavMapBeaconConfigureBuiMessage(label, enabled, color));
        };
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Dispose();
    }
}
