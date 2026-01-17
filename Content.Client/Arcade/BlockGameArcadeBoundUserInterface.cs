using Content.Client.Arcade.UI;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade;

public sealed class BlockGameArcadeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BlockGameArcadeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BlockGameArcadeWindow>();
        _window.OpenCentered();
    }
}
