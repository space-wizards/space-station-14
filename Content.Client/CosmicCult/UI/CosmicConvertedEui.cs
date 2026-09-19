using Content.Client.Eui;

namespace Content.Client.CosmicCult.UI;

public sealed class CosmicConvertedEui : BaseEui
{
    private readonly CosmicConvertedMenu _menu;

    public CosmicConvertedEui()
    {
        _menu = new CosmicConvertedMenu();
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
