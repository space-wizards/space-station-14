using Content.Client.Eui;

namespace Content.Client._Goobstation.Heretic.UI;

public sealed class HellMemoryEui : BaseEui
{
    private readonly HellMemoryMenu _menu;

    public HellMemoryEui()
    {
        _menu = new HellMemoryMenu();
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
