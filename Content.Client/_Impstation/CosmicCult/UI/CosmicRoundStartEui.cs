using Content.Client.Eui;

namespace Content.Client._Impstation.CosmicCult.UI;

public sealed class CosmicRoundStartEui : BaseEui
{
    private readonly CosmicRoundStartMenu _menu;

    public CosmicRoundStartEui()
    {
        _menu = new CosmicRoundStartMenu();
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
