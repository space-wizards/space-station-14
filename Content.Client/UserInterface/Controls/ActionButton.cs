using Content.Client.HUD;
using Content.Client.Stylesheets;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls;

public sealed class ActionButton : SlotControl
{
    private readonly Label _label;
    public readonly TextureRect Icon;

    public BoundKeyFunction? KeyBind
    {
        set
        {
            _keybind = value;
            if (_keybind != null)
            {
                _label.Text = BoundKeyHelper.ShortKeyName(_keybind.Value);
            }
        }
    }

    private BoundKeyFunction? _keybind;
    private Texture? IconTexture { get => Icon.Texture; set => Icon.Texture = value;}
    public ActionButton()
    {
        ButtonRect.Modulate = new(255, 255, 255, 150);
        ButtonTexturePath = "SlotBackground";
        Icon = new TextureRect
        {
            TextureScale = (2, 2)
        };
        AddChild(Icon);
        Icon.Modulate = new(255, 255, 255, 150);
        _label = new Label
        {
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Top
        };

        _label.FontColorOverride = Theme.ResolveColorOrSpecified("whiteText");
        AddChild(_label);
    }

    public void UpdateButtonData(IEntityManager entityManager, ActionType action)
    {
        if (action.Provider == null || !entityManager.TryGetComponent(action.Provider.Value, out SpriteComponent sprite))
        {
            if (action.Icon != null)
            {
                IconTexture = action.Icon.Frame0();
            }
            SpriteView.Sprite = null;
        }
        else
        {
            SpriteView.Sprite = sprite;
        }

    }
}
