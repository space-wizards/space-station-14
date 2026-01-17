using Content.Client.Arcade.UI;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade;

public sealed class SpaceVillainArcadeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SpaceVillainArcadeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SpaceVillainArcadeWindow>();
        _window.OpenCentered();
    }
}
