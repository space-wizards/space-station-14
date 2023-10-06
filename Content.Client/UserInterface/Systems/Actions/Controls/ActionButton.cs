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
using Robust.Client.Utility;
using Robust.Shared.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.Controls.TextureRect;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.UserInterface.Systems.Actions.Controls;

public sealed class ActionButton : Control, IEntityControl
{
    private IEntityManager? _entities;

    private ActionUIController Controller => UserInterfaceManager.GetUIController<ActionUIController>();
    private IEntityManager Entities => _entities ??= IoCManager.Resolve<IEntityManager>();
    private ActionsSystem Actions => Entities.System<ActionsSystem>();
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

    public EntityUid? ActionId { get; private set; }
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

        TooltipSupplier = SupplyTooltip;
    }

    protected override void OnThemeUpdated()
    {
        base.OnThemeUpdated();
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
        if (!Entities.TryGetComponent(ActionId, out MetaDataComponent? metadata))
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
        if (!Actions.TryGetActionData(ActionId, out var action) ||
            action is not {EntityIcon: { } entity} ||
            !Entities.HasComponent<SpriteComponent>(entity))
        {
            _bigItemSpriteView.Visible = false;
            _bigItemSpriteView.SetEntity(null);
            _smallItemSpriteView.Visible = false;
            _smallItemSpriteView.SetEntity(null);
        }
        else
        {
            switch (action.ItemIconStyle)
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
        if (!Actions.TryGetActionData(ActionId, out var action) || texture == null)
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

        if (!Actions.TryGetActionData(ActionId, out var action))
        {
            SetActionIcon(null);
            return;
        }

        if ((Controller.SelectingTargetFor == ActionId || action.Toggled) && action.IconOn != null)
            SetActionIcon(action.IconOn.Frame0());
        else
            SetActionIcon(action.Icon?.Frame0());
    }

    public bool TryReplaceWith(EntityUid actionId)
    {
        if (Locked)
        {
            return false;
        }

        UpdateData(actionId);
        return true;
    }

    public void UpdateData(EntityUid actionId)
    {
        ActionId = actionId;
        Label.Visible = true;
        UpdateIcons();
    }

    public void ClearData()
    {
        ActionId = null;
        Cooldown.Visible = false;
        Cooldown.Progress = 1;
        Label.Visible = false;
        UpdateIcons();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!Actions.TryGetActionData(ActionId, out var action))
        {
            return;
        }

        if (action.Cooldown != null)
        {
            Cooldown.FromTime(action.Cooldown.Value.Start, action.Cooldown.Value.End);
        }

        if (ActionId != null && _toggled != action.Toggled)
        {
            _toggled = action.Toggled;
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
        if (!Actions.TryGetActionData(ActionId, out var action) || action is not {Enabled: true})
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
        if (!Actions.TryGetActionData(ActionId, out var action))
        {
            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
            return;
        }

        // show a hover only if the action is usable or another action is being dragged on top of this
        if (_beingHovered && (Controller.IsDragging || action.Enabled))
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
        if (action.Toggled || Controller.SelectingTargetFor == ActionId)
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

    EntityUid? IEntityControl.UiEntity => ActionId;
}
