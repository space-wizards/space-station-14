using Content.Client.Eui;

namespace Content.Client._Impstation.CosmicCult.UI;

public sealed class CosmicDeconvertedEui : BaseEui
{
    private readonly CosmicDeconvertedMenu _menu;

    public CosmicDeconvertedEui()
    {
        _menu = new CosmicDeconvertedMenu();
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
