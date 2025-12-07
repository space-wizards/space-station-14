using System.Numerics;
using Content.Client.Actions;
using Content.Client.Actions.UI;
using Content.Client.Cooldown;
using Content.Client.Stylesheets;
using Content.Shared.Actions.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
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
    public const string StyleClassActionHighlightRect = "ActionHighlightRect";

    private IEntityManager _entities;
    private IPlayerManager _player;
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

    public Entity<ActionComponent>? Action { get; private set; }
    public bool Locked { get; set; }

    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionPressed;
    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionUnpressed;
    public event Action<ActionButton>? ActionFocusExited;

    public ActionButton(IEntityManager entities, SpriteSystem? spriteSys = null, ActionUIController? controller = null)
    {
        // TODO why is this constructor so slooooow. The rest of the code is fine

        _entities = entities;
        _player = IoCManager.Resolve<IPlayerManager>();
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
            StyleClasses = { StyleClassActionHighlightRect },
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

        OnKeyBindDown += OnPressed;
        OnKeyBindUp += OnUnpressed;

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
        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.UIRightClick)
            return;

        if (args.Function == EngineKeyFunctions.UIRightClick)
            Depress(args, true);

        ActionPressed?.Invoke(args, this);
    }

    private void OnUnpressed(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.UIRightClick)
            return;

        if (args.Function == EngineKeyFunctions.UIRightClick)
            Depress(args, false);

        ActionUnpressed?.Invoke(args, this);
    }

    private Control? SupplyTooltip(Control sender)
    {
        if (!_entities.TryGetComponent(Action, out MetaDataComponent? metadata))
            return null;

        var name = FormattedMessage.FromMarkupPermissive(Loc.GetString(metadata.EntityName));
        var desc = FormattedMessage.FromMarkupPermissive(Loc.GetString(metadata.EntityDescription));

        if (_player.LocalEntity is null)
            return null;

        var ev = new ExaminedEvent(desc, Action.Value, _player.LocalEntity.Value, true, !desc.IsEmpty);
        _entities.EventBus.RaiseLocalEvent(Action.Value.Owner, ev);

        var newDesc = ev.GetTotalMessage();

        return new ActionAlertTooltip(name, newDesc);
    }

    protected override void ControlFocusExited()
    {
        ActionFocusExited?.Invoke(this);
    }

    private void UpdateItemIcon()
    {
        if (Action?.Comp is not {EntityIcon: { } entity} ||
            !_entities.HasComponent<SpriteComponent>(entity))
        {
            _bigItemSpriteView.Visible = false;
            _bigItemSpriteView.SetEntity(null);
            _smallItemSpriteView.Visible = false;
            _smallItemSpriteView.SetEntity(null);
        }
        else
        {
            switch (Action?.Comp.ItemIconStyle)
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
        if (Action?.Comp is not {} action || texture == null)
        {
            _bigActionIcon.Texture = null;
            _bigActionIcon.Visible = false;
            _smallActionIcon.Texture = null;
            _smallActionIcon.Visible = false;
        }
        else if (action.EntityIcon != null && action.ItemIconStyle == ItemActionIconStyle.BigItem)
        {
            _smallActionIcon.Texture = texture;
            _smallActionIcon.Modulate = action.IconColor;
            _smallActionIcon.Visible = true;
            _bigActionIcon.Texture = null;
            _bigActionIcon.Visible = false;
        }
        else
        {
            _bigActionIcon.Texture = texture;
            _bigActionIcon.Modulate = action.IconColor;
            _bigActionIcon.Visible = true;
            _smallActionIcon.Texture = null;
            _smallActionIcon.Visible = false;
        }
    }

    public void UpdateIcons()
    {
        UpdateItemIcon();
        UpdateBackground();

        if (Action is not {} action)
        {
            SetActionIcon(null);
            return;
        }

        _controller ??= UserInterfaceManager.GetUIController<ActionUIController>();
        _spriteSys ??= _entities.System<SpriteSystem>();
        var icon = action.Comp.Icon;
        if (_controller.SelectingTargetFor == action || action.Comp.Toggled)
        {
            if (action.Comp.IconOn is {} iconOn)
                icon = iconOn;

            if (action.Comp.BackgroundOn is {} background)
                _buttonBackgroundTexture = _spriteSys.Frame0(background);
        }
        else
        {
            _buttonBackgroundTexture = Theme.ResolveTexture("SlotBackground");
        }

        SetActionIcon(icon != null ? _spriteSys.Frame0(icon) : null);
    }

    public void UpdateBackground()
    {
        _controller ??= UserInterfaceManager.GetUIController<ActionUIController>();
        if (Action != null ||
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
            return false;

        UpdateData(actionId, system);
        return true;
    }

    public void UpdateData(EntityUid? actionId, ActionsSystem system)
    {
        Action = system.GetAction(actionId);

        Label.Visible = Action != null;
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

        UpdateBackground();

        Cooldown.Visible = Action?.Comp.Cooldown != null;
        if (Action?.Comp is not {} action)
            return;

        if (action.Cooldown is {} cooldown)
            Cooldown.FromTime(cooldown.Start, cooldown.End);

        if (_toggled != action.Toggled)
            _toggled = action.Toggled;
    }

    protected override void MouseEntered()
    {
        base.MouseEntered();

        UserInterfaceManager.HoverSound();
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
        if (Action?.Comp is not {Enabled: true})
            return;

        _depressed = depress;
        DrawModeChanged();
    }

    public void DrawModeChanged()
    {
        _controller ??= UserInterfaceManager.GetUIController<ActionUIController>();
        HighlightRect.Visible = _beingHovered && (Action != null || _controller.IsDragging);

        // always show the normal empty button style if no action in this slot
        if (Action?.Comp is not {} action)
        {
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
            return;
        }

        // show a hover only if the action is usable or another action is being dragged on top of this
        if (_beingHovered && (_controller.IsDragging || action.Enabled))
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
        if (action.Toggled || _controller.SelectingTargetFor == Action?.Owner)
        {
            // when there's a toggle sprite, we're showing that sprite instead of highlighting this slot
            SetOnlyStylePseudoClass(action.IconOn != null
                ? ContainerButton.StylePseudoClassNormal
                : ContainerButton.StylePseudoClassPressed);
            return;
        }

        if (!action.Enabled)
        {
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassDisabled);
            return;
        }

        SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
    }

    EntityUid? IEntityControl.UiEntity => Action;
}
