using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.HUD.UI;

internal sealed class TopButton : ContainerButton
{
    public const string StyleClassLabelTopButton = "topButtonLabel";
    public const string StyleClassRedTopButton = "topButtonLabel";
    private const float CustomTooltipDelay = 0.4f;

    private static readonly Color ColorNormal = Color.FromHex("#7b7e9e");
    private static readonly Color ColorRedNormal = Color.FromHex("#FEFEFE");
    private static readonly Color ColorHovered = Color.FromHex("#9699bb");
    private static readonly Color ColorRedHovered = Color.FromHex("#FFFFFF");
    private static readonly Color ColorPressed = Color.FromHex("#789B8C");

    private const float VertPad = 8f;
    private Color NormalColor => HasStyleClass(StyleClassRedTopButton) ? ColorRedNormal : ColorNormal;
    private Color HoveredColor => HasStyleClass(StyleClassRedTopButton) ? ColorRedHovered : ColorHovered;

    private readonly TextureRect _textureRect;
    private readonly Label _label;
    private readonly BoundKeyFunction _function;
    private readonly IInputManager _inputManager;

    public TopButton(Texture texture, BoundKeyFunction function, IInputManager inputManager)
    {
        _function = function;
        _inputManager = inputManager;
        TooltipDelay = CustomTooltipDelay;

        AddChild(
            new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Children =
                {
                    (_textureRect = new TextureRect
                    {
                        TextureScale = (0.5f, 0.5f),
                        Texture = texture,
                        HorizontalAlignment = HAlignment.Center,
                        VerticalAlignment = VAlignment.Center,
                        VerticalExpand = true,
                        Margin = new Thickness(0, VertPad),
                        ModulateSelfOverride = NormalColor,
                        Stretch = TextureRect.StretchMode.KeepCentered
                    }),
                    (_label = new Label
                    {
                        Text = ShortKeyName(_function),
                        HorizontalAlignment = HAlignment.Center,
                        ModulateSelfOverride = NormalColor,
                        StyleClasses = {StyleClassLabelTopButton}
                    })
                }
            }
        );

        ToggleMode = true;
    }

    protected override void EnteredTree()
    {
        _inputManager.OnKeyBindingAdded += OnKeyBindingChanged;
        _inputManager.OnKeyBindingRemoved += OnKeyBindingChanged;
        _inputManager.OnInputModeChanged += OnKeyBindingChanged;
    }

    protected override void ExitedTree()
    {
        _inputManager.OnKeyBindingAdded -= OnKeyBindingChanged;
        _inputManager.OnKeyBindingRemoved -= OnKeyBindingChanged;
        _inputManager.OnInputModeChanged -= OnKeyBindingChanged;
    }


    private void OnKeyBindingChanged(IKeyBinding obj)
    {
        _label.Text = ShortKeyName(_function);
    }

    private void OnKeyBindingChanged()
    {
        _label.Text = ShortKeyName(_function);
    }

    private string ShortKeyName(BoundKeyFunction keyFunction)
    {
        // need to use shortened key names so they fit in the buttons.
        return TryGetShortKeyName(keyFunction, out var name) ? Loc.GetString(name) : " ";
    }

    private bool TryGetShortKeyName(BoundKeyFunction keyFunction, [NotNullWhen(true)] out string? name)
    {
        if (_inputManager.TryGetKeyBinding(keyFunction, out var binding))
        {
            // can't possibly fit a modifier key in the top button, so omit it
            var key = binding.BaseKey;
            if (binding.Mod1 != Keyboard.Key.Unknown || binding.Mod2 != Keyboard.Key.Unknown ||
                binding.Mod3 != Keyboard.Key.Unknown)
            {
                name = null;
                return false;
            }

            name = null;
            name = key switch
            {
                Keyboard.Key.Apostrophe => "'",
                Keyboard.Key.Comma => ",",
                Keyboard.Key.Delete => "Del",
                Keyboard.Key.Down => "Dwn",
                Keyboard.Key.Escape => "Esc",
                Keyboard.Key.Equal => "=",
                Keyboard.Key.Home => "Hom",
                Keyboard.Key.Insert => "Ins",
                Keyboard.Key.Left => "Lft",
                Keyboard.Key.Menu => "Men",
                Keyboard.Key.Minus => "-",
                Keyboard.Key.Num0 => "0",
                Keyboard.Key.Num1 => "1",
                Keyboard.Key.Num2 => "2",
                Keyboard.Key.Num3 => "3",
                Keyboard.Key.Num4 => "4",
                Keyboard.Key.Num5 => "5",
                Keyboard.Key.Num6 => "6",
                Keyboard.Key.Num7 => "7",
                Keyboard.Key.Num8 => "8",
                Keyboard.Key.Num9 => "9",
                Keyboard.Key.Pause => "||",
                Keyboard.Key.Period => ".",
                Keyboard.Key.Return => "Ret",
                Keyboard.Key.Right => "Rgt",
                Keyboard.Key.Slash => "/",
                Keyboard.Key.Space => "Spc",
                Keyboard.Key.Tab => "Tab",
                Keyboard.Key.Tilde => "~",
                Keyboard.Key.BackSlash => "\\",
                Keyboard.Key.BackSpace => "Bks",
                Keyboard.Key.LBracket => "[",
                Keyboard.Key.MouseButton4 => "M4",
                Keyboard.Key.MouseButton5 => "M5",
                Keyboard.Key.MouseButton6 => "M6",
                Keyboard.Key.MouseButton7 => "M7",
                Keyboard.Key.MouseButton8 => "M8",
                Keyboard.Key.MouseButton9 => "M9",
                Keyboard.Key.MouseLeft => "ML",
                Keyboard.Key.MouseMiddle => "MM",
                Keyboard.Key.MouseRight => "MR",
                Keyboard.Key.NumpadDecimal => "N.",
                Keyboard.Key.NumpadDivide => "N/",
                Keyboard.Key.NumpadEnter => "Ent",
                Keyboard.Key.NumpadMultiply => "*",
                Keyboard.Key.NumpadNum0 => "0",
                Keyboard.Key.NumpadNum1 => "1",
                Keyboard.Key.NumpadNum2 => "2",
                Keyboard.Key.NumpadNum3 => "3",
                Keyboard.Key.NumpadNum4 => "4",
                Keyboard.Key.NumpadNum5 => "5",
                Keyboard.Key.NumpadNum6 => "6",
                Keyboard.Key.NumpadNum7 => "7",
                Keyboard.Key.NumpadNum8 => "8",
                Keyboard.Key.NumpadNum9 => "9",
                Keyboard.Key.NumpadSubtract => "N-",
                Keyboard.Key.PageDown => "PgD",
                Keyboard.Key.PageUp => "PgU",
                Keyboard.Key.RBracket => "]",
                Keyboard.Key.SemiColon => ";",
                _ => DefaultShortKeyName(keyFunction)
            };
            return name != null;
        }

        name = null;
        return false;
    }

    private string? DefaultShortKeyName(BoundKeyFunction keyFunction)
    {
        var name = FormattedMessage.EscapeText(_inputManager.GetKeyFunctionButtonString(keyFunction));
        return name.Length > 3 ? null : name;
    }

    protected override void StylePropertiesChanged()
    {
        // colors of children depend on style, so ensure we update when style is changed
        base.StylePropertiesChanged();
        UpdateChildColors();
    }

    private void UpdateChildColors()
    {
        if (_label == null || _textureRect == null) return;
        switch (DrawMode)
        {
            case DrawModeEnum.Normal:
                _textureRect.ModulateSelfOverride = NormalColor;
                _label.ModulateSelfOverride = NormalColor;
                break;

            case DrawModeEnum.Pressed:
                _textureRect.ModulateSelfOverride = ColorPressed;
                _label.ModulateSelfOverride = ColorPressed;
                break;

            case DrawModeEnum.Hover:
                _textureRect.ModulateSelfOverride = HoveredColor;
                _label.ModulateSelfOverride = HoveredColor;
                break;

            case DrawModeEnum.Disabled:
                break;
        }
    }


    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateChildColors();
    }
}
