using Content.Client.Actions.UI;
using Content.Client.Cooldown;
using Content.Client.Stylesheets;
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
    private ActionUIController Controller => UserInterfaceManager.GetUIController<ActionUIController>();
    private bool _beingHovered;
    private bool _depressed;
    private bool _toggled;

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
    public readonly PanelContainer HighlightRect;
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
            TextureScale = new Vector2(2, 2)
        };
        HighlightRect = new PanelContainer
        {
            StyleClasses = {StyleNano.StyleClassHandSlotHighlight},
            MinSize = (32, 32),
            Visible = false
        };
        Icon = new TextureRect
        {
            Name = "Icon",
            TextureScale = new Vector2(2, 2),
            MaxSize = (64, 64),
            Stretch = TextureRect.StretchMode.Scale
        };
        Label = new Label
        {
            Name = "Label",
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Top,
            Margin = new Thickness(5, 0, 0, 0)
        };
        Sprite = new SpriteView
        {
            Name = "Sprite",
            OverrideDirection = Direction.South
        };
        Cooldown = new CooldownGraphic {Visible = false};

        AddChild(Button);
        AddChild(HighlightRect);
        AddChild(Icon);
        AddChild(Label);
        AddChild(Sprite);
        AddChild(Cooldown);

        Button.Modulate = new Color(255, 255, 255, 150);
        Icon.Modulate = new Color(255, 255, 255, 150);

        OnThemeUpdated();
        OnThemeUpdated();

        OnKeyBindDown += args =>
        {
            Depress(args, true);
            OnPressed(args);
        };
        OnKeyBindUp += args =>
        {
            Depress(args, false);
            OnUnpressed(args);
        };

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

        if (action.Icon != null)
        {
            IconTexture = GetIcon();
            Sprite.Sprite = null;
            return;
        }

        if (action.Provider == null ||
            !entityManager.TryGetComponent(action.Provider.Value, out SpriteComponent? sprite))
        {
            return;
        }

        IconTexture = null;
        Sprite.Sprite = sprite;
    }

    public void ClearData()
    {
        Action = null;
        IconTexture = null;
        Sprite.Sprite = null;
        Cooldown.Visible = false;
        Cooldown.Progress = 1;
    }

    private Texture? GetIcon()
    {
        if (Action == null)
            return null;

        return _toggled ? (Action.IconOn ?? Action.Icon)?.Frame0() : Action.Icon?.Frame0();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Action?.Cooldown != null)
        {
            Cooldown.FromTime(Action.Cooldown.Value.Start, Action.Cooldown.Value.End);
        }

        if (Action != null && _toggled != Action.Toggled)
        {
            _toggled = Action.Toggled;
            IconTexture = GetIcon();
        }
    }

    protected override void MouseEntered()
    {
        base.MouseEntered();

        _beingHovered = true;
        DrawModeChanged();
    }

    protected override void MouseExited()
    {
        base.MouseExited();

        _beingHovered = false;
        DrawModeChanged();
    }

    /// <summary>
    /// Press this button down. If it was depressed and now set to not depressed, will
    /// trigger the action.
    /// </summary>
    public void Depress(GUIBoundKeyEventArgs args, bool depress)
    {
        // action can still be toggled if it's allowed to stay selected
        if (Action is not {Enabled: true})
            return;

        if (_depressed && !depress)
        {
            // fire the action
            OnUnpressed(args);
        }

        _depressed = depress;
        DrawModeChanged();
    }

    public void DrawModeChanged()
    {
        HighlightRect.Visible = _beingHovered;

        // always show the normal empty button style if no action in this slot
        if (Action == null)
        {
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
            return;
        }

        // show a hover only if the action is usable or another action is being dragged on top of this
        if (_beingHovered && (Controller.IsDragging || Action.Enabled))
        {
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassHover);
        }

        // it's only depress-able if it's usable, so if we're depressed
        // show the depressed style
        if (_depressed)
        {
            HighlightRect.Visible = false;
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassPressed);
            return;
        }

        // if it's toggled on, always show the toggled on style (currently same as depressed style)
        if (Action.Toggled || Controller.SelectingTargetFor == this)
        {
            // when there's a toggle sprite, we're showing that sprite instead of highlighting this slot
            SetOnlyStylePseudoClass(Action.IconOn != null
                ? ContainerButton.StylePseudoClassNormal
                : ContainerButton.StylePseudoClassPressed);
            return;
        }

        if (!Action.Enabled)
        {
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassDisabled);
            return;
        }

        SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
    }
}
