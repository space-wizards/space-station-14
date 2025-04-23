using Content.Client.Eui;

namespace Content.Client._Goobstation.Heretic.UI;

public sealed class GhoulNotifEui : BaseEui
{
    private readonly GhoulNotifMenu _menu;

    public GhoulNotifEui()
    {
        _menu = new GhoulNotifMenu();
    }

    public override void Opened()
    {
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _menu.Close();
    }
}
