using Robust.Client.UserInterface;

namespace Content.Client.Arcade.UI;

/// <summary>
///
/// </summary>
public sealed class LastFighterArcadeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private LastFighterArcadeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LastFighterArcadeWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OpenCentered();
    }
}
