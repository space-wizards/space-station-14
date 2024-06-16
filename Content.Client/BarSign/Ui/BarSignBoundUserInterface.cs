using Content.Shared.BarSign;
using JetBrains.Annotations;

namespace Content.Client.BarSign.Ui;

[UsedImplicitly]
public sealed class BarSignBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BarSignMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = new(Owner);

        _menu.OnSignSelected += id =>
        {
            SendMessage(new SetBarSignMessage(id));
        };

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}

