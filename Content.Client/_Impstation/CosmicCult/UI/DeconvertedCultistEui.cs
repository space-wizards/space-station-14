using Content.Client.Eui;

namespace Content.Client._Impstation.CosmicCult.UI;

public sealed class DeconvertedCultistEui : BaseEui
{
    private readonly CosmicDeconvertedMenu _menu;

    public DeconvertedCultistEui()
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
