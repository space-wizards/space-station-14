using Content.Client.Actions.UI;
using Content.Client.Cooldown;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Actions.Controls;

public sealed class ActionButton : Control
{
    public BoundKeyFunction? KeyBind
    {
        set
        {
            _keybind = value;
            if (_keybind != null)
            {
                Label.Text = BoundKeyHelper.ShortKeyName(_keybind.Value);
            }
        }
    }

    private BoundKeyFunction? _keybind;

    public readonly TextureRect Button;
    public readonly TextureRect Icon;
    public readonly Label Label;
    public readonly SpriteView Sprite;
    public readonly CooldownGraphic Cooldown;

    public Texture? IconTexture
    {
        get => Icon.Texture;
        private set => Icon.Texture = value;
    }

    public ActionType? Action { get; private set; }
    public bool Locked { get; set; }

    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionPressed;
    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionUnpressed;
    public event Action<ActionButton>? ActionFocusExited;

    public ActionButton()
    {
        MouseFilter = MouseFilterMode.Pass;
        Button = new TextureRect
        {
            Name = "Button",
            TextureScale = new Vector2(2,2)
        };
        Icon = new TextureRect
        {
            Name = "Icon",
            TextureScale = new Vector2(2,2),
            MaxSize = (64, 64),
            Stretch = TextureRect.StretchMode.Scale
        };
        Label = new Label
        {
            Name= "Label",
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Top,
            Margin = new Thickness(5, 0,0, 0)
        };
        Sprite = new SpriteView
        {
            Name = "Sprite",
            OverrideDirection = Direction.South
        };
        Cooldown = new CooldownGraphic {Visible = false};

        AddChild(Button);
        AddChild(Icon);
        AddChild(Label);
        AddChild(Sprite);
        AddChild(Cooldown);

        Button.Modulate = new Color(255, 255, 255, 150);
        Icon.Modulate = new Color(255, 255, 255, 150);

        OnThemeUpdated();
        OnThemeUpdated();

        OnKeyBindDown += OnPressed;
        OnKeyBindUp += OnUnpressed;

        TooltipDelay = 0.5f;
        TooltipSupplier = SupplyTooltip;
    }

    protected override void OnThemeUpdated()
    {
        Button.Texture = Theme.ResolveTexture("SlotBackground");
        Label.FontColorOverride = Theme.ResolveColorOrSpecified("whiteText");
    }

    private void OnPressed(GUIBoundKeyEventArgs args)
    {
        ActionPressed?.Invoke(args, this);
    }

    private void OnUnpressed(GUIBoundKeyEventArgs args)
    {
        ActionUnpressed?.Invoke(args, this);
    }

    private Control? SupplyTooltip(Control sender)
    {
        if (Action == null)
            return null;

        var name = FormattedMessage.FromMarkupPermissive(Loc.GetString(Action.DisplayName));
        var decr = FormattedMessage.FromMarkupPermissive(Loc.GetString(Action.Description));

        return new ActionAlertTooltip(name, decr);
    }

    protected override void ControlFocusExited()
    {
        ActionFocusExited?.Invoke(this);
    }

    public bool TryReplaceWith(IEntityManager entityManager, ActionType action)
    {
        if (Locked)
        {
            return false;
        }

        UpdateData(entityManager, action);
        return true;
    }

    public void UpdateData(IEntityManager entityManager, ActionType action)
    {
        Action = action;

        if (action.Provider == null ||
            !entityManager.TryGetComponent(action.Provider.Value, out SpriteComponent? sprite))
        {
            IconTexture = action.Icon?.Frame0();
            Sprite.Sprite = null;
        }
        else
        {
            IconTexture = null;
            Sprite.Sprite = sprite;
        }
    }

    public void ClearData()
    {
        Action = null;
        IconTexture = null;
        Sprite.Sprite = null;
        Cooldown.Visible = false;
        Cooldown.Progress = 1;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Action?.Cooldown != null)
        {
            Cooldown.FromTime(Action.Cooldown.Value.Start, Action.Cooldown.Value.End);
        }
    }
}
