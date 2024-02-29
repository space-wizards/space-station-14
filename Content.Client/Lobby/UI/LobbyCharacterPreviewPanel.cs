using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Handles the entity preview for a character in a particular role.
/// </summary>
public sealed class LobbyCharacterPreviewPanel : Control
{
    private readonly Label _summaryLabel;
    private readonly BoxContainer _loaded;
    private readonly BoxContainer _viewBox;
    private readonly Label _unloaded;

    public LobbyCharacterPreviewPanel()
    {
        IoCManager.InjectDependencies(this);
        var header = new NanoHeading
        {
            Text = Loc.GetString("lobby-character-preview-panel-header")
        };

        CharacterSetupButton = new Button
        {
            Text = Loc.GetString("lobby-character-preview-panel-character-setup-button"),
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
        };

        _summaryLabel = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(3, 3),
        };

        var vBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical
        };
        _unloaded = new Label { Text = Loc.GetString("lobby-character-preview-panel-unloaded-preferences-label") };

        _loaded = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Visible = false
        };
        _viewBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
        };
        var vSpacer = new VSpacer();

        _loaded.AddChild(_summaryLabel);
        _loaded.AddChild(_viewBox);
        _loaded.AddChild(vSpacer);
        _loaded.AddChild(CharacterSetupButton);

        vBox.AddChild(header);
        vBox.AddChild(_loaded);
        vBox.AddChild(_unloaded);
        AddChild(vBox);

        UserInterfaceManager.GetUIController<LobbyUIController>().SetPreviewPanel(this);
    }

    public void SetLoaded(bool value)
    {
        _loaded.Visible = value;
        _unloaded.Visible = !value;
    }

    public void SetSummaryText(string value)
    {
        _summaryLabel.Text = string.Empty;
    }

    public void SetSprite(EntityUid uid)
    {
        _viewBox.DisposeAllChildren();
        var spriteView = new SpriteView
        {
            OverrideDirection = Direction.South,
            Scale = new Vector2(4f, 4f),
            MaxSize = new Vector2(112, 112),
            Stretch = SpriteView.StretchMode.Fill,
        };
        spriteView.SetEntity(uid);
        _viewBox.AddChild(spriteView);
    }

    public Button CharacterSetupButton { get; }
}
