using Content.Client.Actions.UI;
using Content.Client.Cooldown;
using Content.Client.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.Controls.TextureRect;

namespace Content.Client.UserInterface.Systems.Actions.Controls;

public sealed class ActionButton : Control
{
    private ActionUIController Controller => UserInterfaceManager.GetUIController<ActionUIController>();
    private bool _beingHovered;
    private bool _depressed;
    private bool _toggled;
    private bool _spriteViewDirty;

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
    private readonly TextureRect _bigActionIcon;
    private readonly TextureRect _smallActionIcon;
    public readonly Label Label;
    public readonly CooldownGraphic Cooldown;
    private readonly SpriteView _smallItemSpriteView;
    private readonly SpriteView _bigItemSpriteView;

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
        _bigActionIcon = new TextureRect
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Stretch = StretchMode.Scale,
            Visible = false
        };
        _smallActionIcon = new TextureRect
        {
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Bottom,
            Stretch = StretchMode.Scale,
            Visible = false
        };
        Label = new Label
        {
            Name = "Label",
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Top,
            Margin = new Thickness(5, 0, 0, 0)
        };
        _bigItemSpriteView = new SpriteView
        {
            Name = "Big Sprite",
            HorizontalExpand = true,
            VerticalExpand = true,
            Scale = (2, 2),
            SetSize = (64, 64),
            Visible = false,
            OverrideDirection = Direction.South,
        };
        _smallItemSpriteView = new SpriteView
        {
            Name = "Small Sprite",
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Bottom,
            Visible = false,
            OverrideDirection = Direction.South,
        };
        // padding to the left of the small icon
        var paddingBoxItemIcon = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            MinSize = (64, 64)
        };
        paddingBoxItemIcon.AddChild(new Control()
        {
            MinSize = (32, 32),
        });
        paddingBoxItemIcon.AddChild(new Control
        {
            Children =
            {
                _smallActionIcon,
                _smallItemSpriteView
            }
        });
        Cooldown = new CooldownGraphic {Visible = false};

        AddChild(_bigActionIcon);
        AddChild(_bigItemSpriteView);
        AddChild(Button);
        AddChild(HighlightRect);
        AddChild(Label);
        AddChild(Cooldown);
        AddChild(paddingBoxItemIcon);

        Button.Modulate = new Color(255, 255, 255, 150);

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

    private void UpdateItemIcon()
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        if (Action?.EntityIcon != null && !entityManager.EntityExists(Action.EntityIcon))
        {
            // This is almost certainly because a player received/processed their own actions component state before
            // being send the entity in their inventory that enabled this action.

            // Defer updating icons to the next FrameUpdate().
            _spriteViewDirty = true;
            return;
        }

        if (Action?.EntityIcon == null ||
            !entityManager.TryGetComponent(Action.EntityIcon.Value, out SpriteComponent? sprite))
        {
            _bigItemSpriteView.Visible = false;
            _bigItemSpriteView.Sprite = null;
            _smallItemSpriteView.Visible = false;
            _smallItemSpriteView.Sprite = null;
        }
        else
        {
            switch (Action.ItemIconStyle)
            {
                case ItemActionIconStyle.BigItem:
                    _bigItemSpriteView.Visible = true;
                    _bigItemSpriteView.Sprite = sprite;
                    _smallItemSpriteView.Visible = false;
                    _smallItemSpriteView.Sprite = null;
                    break;
                case ItemActionIconStyle.BigAction:

                    _bigItemSpriteView.Visible = false;
                    _bigItemSpriteView.Sprite = null;
                    _smallItemSpriteView.Visible = true;
                    _smallItemSpriteView.Sprite = sprite;
                    break;

                case ItemActionIconStyle.NoItem:

                    _bigItemSpriteView.Visible = false;
                    _bigItemSpriteView.Sprite = null;
                    _smallItemSpriteView.Visible = false;
                    _smallItemSpriteView.Sprite = null;
                    break;
            }
        }
    }

    private void SetActionIcon(Texture? texture)
    {
        if (texture == null || Action == null)
        {
            _bigActionIcon.Texture = null;
            _bigActionIcon.Visible = false;
            _smallActionIcon.Texture = null;
            _smallActionIcon.Visible = false;
        }
        else if (Action.EntityIcon != null && Action.ItemIconStyle == ItemActionIconStyle.BigItem)
        {
            _smallActionIcon.Texture = texture;
            _smallActionIcon.Modulate = Action.IconColor;
            _smallActionIcon.Visible = true;
            _bigActionIcon.Texture = null;
            _bigActionIcon.Visible = false;
        }
        else
        {
            _bigActionIcon.Texture = texture;
            _bigActionIcon.Modulate = Action.IconColor;
            _bigActionIcon.Visible = true;
            _smallActionIcon.Texture = null;
            _smallActionIcon.Visible = false;
        }
    }

    public void UpdateIcons()
    {
        UpdateItemIcon();

        if (Action == null)
        {
            SetActionIcon(null);
            return;
        }

        if ((Controller.SelectingTargetFor == Action || Action.Toggled) && Action.IconOn != null)
            SetActionIcon(Action.IconOn.Frame0());
        else
            SetActionIcon(Action.Icon?.Frame0());
    }

    public bool TryReplaceWith(ActionType action)
    {
        if (Locked)
        {
            return false;
        }

        UpdateData(action);
        return true;
    }

    public void UpdateData(ActionType action)
    {
        Action = action;
        Label.Visible = true;
        UpdateIcons();
    }

    public void ClearData()
    {
        Action = null;
        Cooldown.Visible = false;
        Cooldown.Progress = 1;
        Label.Visible = false;
        UpdateIcons();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_spriteViewDirty)
        {
            _spriteViewDirty = false;
            UpdateIcons();
        }

        if (Action?.Cooldown != null)
        {
            Cooldown.FromTime(Action.Cooldown.Value.Start, Action.Cooldown.Value.End);
        }

        if (Action != null && _toggled != Action.Toggled)
        {
            _toggled = Action.Toggled;
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
        if (Action.Toggled || Controller.SelectingTargetFor == Action)
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
