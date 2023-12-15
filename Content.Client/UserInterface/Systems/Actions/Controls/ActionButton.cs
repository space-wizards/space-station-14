using System.Numerics;
using Content.Client.Actions;
using Content.Client.Actions.UI;
using Content.Client.Cooldown;
using Content.Client.Stylesheets;
using Content.Shared.Actions;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.Controls.TextureRect;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.UserInterface.Systems.Actions.Controls;

public sealed class ActionButton : Control, IEntityControl
{
    private IEntityManager _entities;
    private SpriteSystem? _spriteSys;
    private ActionUIController? _controller;
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
    private readonly TextureRect _bigActionIcon;
    private readonly TextureRect _smallActionIcon;
    public readonly Label Label;
    public readonly CooldownGraphic Cooldown;
    private readonly SpriteView _smallItemSpriteView;
    private readonly SpriteView _bigItemSpriteView;

    private Texture? _buttonBackgroundTexture;

    public EntityUid? ActionId { get; private set; }
    private BaseActionComponent? _action;
    public bool Locked { get; set; }

    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionPressed;
    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionUnpressed;
    public event Action<ActionButton>? ActionFocusExited;

    public ActionButton(IEntityManager entities, SpriteSystem? spriteSys = null, ActionUIController? controller = null)
    {
        // TODO why is this constructor so slooooow. The rest of the code is fine

        _entities = entities;
        _spriteSys = spriteSys;
        _controller = controller;

        MouseFilter = MouseFilterMode.Pass;
        Button = new TextureRect
        {
            Name = "Button",
            TextureScale = new Vector2(2, 2)
        };
        HighlightRect = new PanelContainer
        {
            StyleClasses = {StyleNano.StyleClassHandSlotHighlight},
            MinSize = new Vector2(32, 32),
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
            Scale = new Vector2(2, 2),
            SetSize = new Vector2(64, 64),
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
            MinSize = new Vector2(64, 64)
        };
        paddingBoxItemIcon.AddChild(new Control()
        {
            MinSize = new Vector2(32, 32),
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

        AddChild(Button);
        AddChild(_bigActionIcon);
        AddChild(_bigItemSpriteView);
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

        TooltipSupplier = SupplyTooltip;
    }

    protected override void OnThemeUpdated()
    {
        base.OnThemeUpdated();
        _buttonBackgroundTexture = Theme.ResolveTexture("SlotBackground");
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
        if (!_entities.TryGetComponent(ActionId, out MetaDataComponent? metadata))
            return null;

        var name = FormattedMessage.FromMarkupPermissive(Loc.GetString(metadata.EntityName));
        var decr = FormattedMessage.FromMarkupPermissive(Loc.GetString(metadata.EntityDescription));

        return new ActionAlertTooltip(name, decr);
    }

    protected override void ControlFocusExited()
    {
        ActionFocusExited?.Invoke(this);
    }

    private void UpdateItemIcon()
    {
        if (_action is not {EntityIcon: { } entity} ||
            !_entities.HasComponent<SpriteComponent>(entity))
        {
            _bigItemSpriteView.Visible = false;
            _bigItemSpriteView.SetEntity(null);
            _smallItemSpriteView.Visible = false;
            _smallItemSpriteView.SetEntity(null);
        }
        else
        {
            switch (_action.ItemIconStyle)
            {
                case ItemActionIconStyle.BigItem:
                    _bigItemSpriteView.Visible = true;
                    _bigItemSpriteView.SetEntity(entity);
                    _smallItemSpriteView.Visible = false;
                    _smallItemSpriteView.SetEntity(null);
                    break;
                case ItemActionIconStyle.BigAction:
                    _bigItemSpriteView.Visible = false;
                    _bigItemSpriteView.SetEntity(null);
                    _smallItemSpriteView.Visible = true;
                    _smallItemSpriteView.SetEntity(entity);
                    break;
                case ItemActionIconStyle.NoItem:
                    _bigItemSpriteView.Visible = false;
                    _bigItemSpriteView.SetEntity(null);
                    _smallItemSpriteView.Visible = false;
                    _smallItemSpriteView.SetEntity(null);
                    break;
            }
        }
    }

    private void SetActionIcon(Texture? texture)
    {
        if (_action == null || texture == null)
        {
            _bigActionIcon.Texture = null;
            _bigActionIcon.Visible = false;
            _smallActionIcon.Texture = null;
            _smallActionIcon.Visible = false;
        }
        else if (_action.EntityIcon != null && _action.ItemIconStyle == ItemActionIconStyle.BigItem)
        {
            _smallActionIcon.Texture = texture;
            _smallActionIcon.Modulate = _action.IconColor;
            _smallActionIcon.Visible = true;
            _bigActionIcon.Texture = null;
            _bigActionIcon.Visible = false;
        }
        else
        {
            _bigActionIcon.Texture = texture;
            _bigActionIcon.Modulate = _action.IconColor;
            _bigActionIcon.Visible = true;
            _smallActionIcon.Texture = null;
            _smallActionIcon.Visible = false;
        }
    }

    public void UpdateIcons()
    {
        UpdateItemIcon();
        UpdateBackground();

        if (_action == null)
        {
            SetActionIcon(null);
            return;
        }

        _controller ??= UserInterfaceManager.GetUIController<ActionUIController>();
        _spriteSys ??= _entities.System<SpriteSystem>();
        if ((_controller.SelectingTargetFor == ActionId || _action.Toggled) && _action.IconOn != null)
            SetActionIcon(_spriteSys.Frame0(_action.IconOn));
        else
            SetActionIcon(_action.Icon != null ? _spriteSys.Frame0(_action.Icon) : null);
    }

    public void UpdateBackground()
    {
        _controller ??= UserInterfaceManager.GetUIController<ActionUIController>();
        if (_action != null ||
            _controller.IsDragging && GetPositionInParent() == Parent?.ChildCount - 1)
        {
            Button.Texture = _buttonBackgroundTexture;
        }
        else
        {
            Button.Texture = null;
        }
    }

    public bool TryReplaceWith(EntityUid actionId, ActionsSystem system)
    {
        if (Locked)
        {
            return false;
        }

        UpdateData(actionId, system);
        return true;
    }

    public void UpdateData(EntityUid? actionId, ActionsSystem system)
    {
        ActionId = actionId;
        system.TryGetActionData(actionId, out _action);
        Label.Visible = actionId != null;
        UpdateIcons();
    }

    public void ClearData()
    {
        ActionId = null;
        _action = null;
        Cooldown.Visible = false;
        Cooldown.Progress = 1;
        Label.Visible = false;
        UpdateIcons();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdateBackground();

        Cooldown.Visible = _action != null && _action.Cooldown != null;
        if (_action == null)
            return;

        if (_action.Cooldown != null)
        {
            Cooldown.FromTime(_action.Cooldown.Value.Start, _action.Cooldown.Value.End);
        }

        if (ActionId != null && _toggled != _action.Toggled)
        {
            _toggled = _action.Toggled;
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
        if (_action is not {Enabled: true})
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
        _controller ??= UserInterfaceManager.GetUIController<ActionUIController>();
        HighlightRect.Visible = _beingHovered && (_action != null || _controller.IsDragging);

        // always show the normal empty button style if no action in this slot
        if (_action == null)
        {
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
            return;
        }

        // show a hover only if the action is usable or another action is being dragged on top of this
        if (_beingHovered && (_controller.IsDragging || _action!.Enabled))
        {
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassHover);
        }

        // it's only depress-able if it's usable, so if we're depressed
        // show the depressed style
        if (_depressed && !_beingHovered)
        {
            HighlightRect.Visible = false;
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassPressed);
            return;
        }

        // if it's toggled on, always show the toggled on style (currently same as depressed style)
        if (_action.Toggled || _controller.SelectingTargetFor == ActionId)
        {
            // when there's a toggle sprite, we're showing that sprite instead of highlighting this slot
            SetOnlyStylePseudoClass(_action.IconOn != null
                ? ContainerButton.StylePseudoClassNormal
                : ContainerButton.StylePseudoClassPressed);
            return;
        }

        if (!_action.Enabled)
        {
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassDisabled);
            return;
        }

        SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
    }

    EntityUid? IEntityControl.UiEntity => ActionId;
}
