using Content.Client.Eui;

namespace Content.Client.CosmicCult.UI;

public sealed class CosmicInfluenceEui : BaseEui
{
    private readonly CosmicInfluenceMenu _menu;

    public CosmicInfluenceEui()
    {
        _menu = new CosmicInfluenceMenu();
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
