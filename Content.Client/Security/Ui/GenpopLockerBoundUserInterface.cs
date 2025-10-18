using Content.Shared.Security.Components;
using JetBrains.Annotations;

namespace Content.Client.Security.Ui;

[UsedImplicitly]
public sealed class GenpopLockerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private GenpopLockerMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = new(Owner, EntMan);

        _menu.OnConfigurationComplete += (name, time, crime) =>
        {
            SendMessage(new GenpopLockerIdConfiguredMessage(name, time, crime));
            Close();
        };

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Orphan();
        _menu = null;
    }
}

