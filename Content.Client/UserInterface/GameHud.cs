using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Input;
using Content.Shared.Prototypes.HUD;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.Input.Keyboard.Key;
using Control = Robust.Client.UserInterface.Control;
using LC = Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     Responsible for laying out the default game HUD.
    /// </summary>
    public interface IGameHud
    {
        Control RootControl { get; }

        // Escape top button.
        bool EscapeButtonDown { get; set; }
        Action<bool>? EscapeButtonToggled { get; set; }

        // Character top button.
        bool CharacterButtonDown { get; set; }
        bool CharacterButtonVisible { get; set; }
        Action<bool>? CharacterButtonToggled { get; set; }

        // Inventory top button.
        bool InventoryButtonDown { get; set; }
        bool InventoryButtonVisible { get; set; }

        // Crafting top button.
        bool CraftingButtonDown { get; set; }
        bool CraftingButtonVisible { get; set; }
        Action<bool>? CraftingButtonToggled { get; set; }

        // Actions top button.
        bool ActionsButtonDown { get; set; }
        bool ActionsButtonVisible { get; set; }
        Action<bool>? ActionsButtonToggled { get; set; }

        // Admin top button.
        bool AdminButtonDown { get; set; }
        bool AdminButtonVisible { get; set; }
        Action<bool>? AdminButtonToggled { get; set; }

        // Sandbox top button.
        bool SandboxButtonDown { get; set; }
        bool SandboxButtonVisible { get; set; }
        Action<bool>? SandboxButtonToggled { get; set; }

        Control HandsContainer { get; }
        Control SuspicionContainer { get; }
        Control BottomLeftInventoryQuickButtonContainer { get; }
        Control BottomRightInventoryQuickButtonContainer { get; }
        Control TopInventoryQuickButtonContainer { get; }

        bool CombatPanelVisible { get; set; }
        TargetingZone TargetingZone { get; set; }
        Action<TargetingZone>? OnTargetingZoneChanged { get; set; }

        Control VoteContainer { get; }

        void AddTopNotification(TopNotification notification);

        Texture GetHudTexture(string path);

        bool ValidateHudTheme(int idx);

        // Init logic.
        void Initialize();
    }

    internal sealed class GameHud : IGameHud
    {
        private HBoxContainer _topButtonsContainer = default!;
        private TopButton _buttonEscapeMenu = default!;
        private TopButton _buttonInfo = default!;
        private TopButton _buttonCharacterMenu = default!;
        private TopButton _buttonInventoryMenu = default!;
        private TopButton _buttonCraftingMenu = default!;
        private TopButton _buttonActionsMenu = default!;
        private TopButton _buttonAdminMenu = default!;
        private TopButton _buttonSandboxMenu = default!;
        private InfoWindow _infoWindow = default!;
        private TargetingDoll _targetingDoll = default!;
        private VBoxContainer _combatPanelContainer = default!;
        private VBoxContainer _topNotificationContainer = default!;

        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly INetConfigurationManager _configManager = default!;

        public Control HandsContainer { get; private set; } = default!;
        public Control SuspicionContainer { get; private set; } = default!;
        public Control TopInventoryQuickButtonContainer { get; private set; } = default!;
        public Control BottomLeftInventoryQuickButtonContainer { get; private set; } = default!;
        public Control BottomRightInventoryQuickButtonContainer { get; private set; } = default!;

        public bool CombatPanelVisible
        {
            get => _combatPanelContainer.Visible;
            set => _combatPanelContainer.Visible = value;
        }

        public TargetingZone TargetingZone
        {
            get => _targetingDoll.ActiveZone;
            set => _targetingDoll.ActiveZone = value;
        }
        public Action<TargetingZone>? OnTargetingZoneChanged { get; set; }

        public void AddTopNotification(TopNotification notification)
        {
            _topNotificationContainer.AddChild(notification);
        }

        public bool ValidateHudTheme(int idx)
        {
            if (!_prototypeManager.TryIndex(idx.ToString(), out HudThemePrototype? _))
            {
                Logger.ErrorS("hud", "invalid HUD theme id {0}, using different theme",
                    idx);
                var proto = _prototypeManager.EnumeratePrototypes<HudThemePrototype>().FirstOrDefault();
                if (proto == null)
                {
                    throw new NullReferenceException("No valid HUD prototypes!");
                }
                var id = int.Parse(proto.ID);
                _configManager.SetCVar(CCVars.HudTheme, id);
                return false;
            }
            return true;
        }

        public Texture GetHudTexture(string path)
        {
            var id = _configManager.GetCVar<int>("hud.theme");
            var dir = string.Empty;
            if (!_prototypeManager.TryIndex(id.ToString(), out HudThemePrototype? proto))
            {
                throw new ArgumentOutOfRangeException();
            }
            dir = proto.Path;

            var resourcePath = (new ResourcePath("/Textures/Interface/Inventory") / dir) / path;
            return _resourceCache.GetTexture(resourcePath);
        }

        public void Initialize()
        {
            RootControl = new LC { Name = "AAAAAAAAAAAAAAAAAAAAAA"};
            LC.SetAnchorPreset(RootControl, LC.LayoutPreset.Wide);

            var escapeTexture = _resourceCache.GetTexture("/Textures/Interface/hamburger.svg.192dpi.png");
            var characterTexture = _resourceCache.GetTexture("/Textures/Interface/character.svg.192dpi.png");
            var inventoryTexture = _resourceCache.GetTexture("/Textures/Interface/inventory.svg.192dpi.png");
            var craftingTexture = _resourceCache.GetTexture("/Textures/Interface/hammer.svg.192dpi.png");
            var actionsTexture = _resourceCache.GetTexture("/Textures/Interface/fist.svg.192dpi.png");
            var adminTexture = _resourceCache.GetTexture("/Textures/Interface/gavel.svg.192dpi.png");
            var infoTexture = _resourceCache.GetTexture("/Textures/Interface/info.svg.192dpi.png");
            var sandboxTexture = _resourceCache.GetTexture("/Textures/Interface/sandbox.svg.192dpi.png");

            _topButtonsContainer = new HBoxContainer
            {
                SeparationOverride = 8
            };

            RootControl.AddChild(_topButtonsContainer);

            LC.SetAnchorAndMarginPreset(_topButtonsContainer, LC.LayoutPreset.TopLeft,
                margin: 10);

            // the icon textures here should all have the same image height (32) but different widths, so in order to ensure
            // the buttons themselves are consistent widths we set a common custom min size
            Vector2 topMinSize = (42, 64);

            // Escape
            _buttonEscapeMenu = new TopButton(escapeTexture, EngineKeyFunctions.EscapeMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open escape menu."),
                MinSize = (70, 64),
                StyleClasses = {StyleBase.ButtonOpenRight}
            };

            _topButtonsContainer.AddChild(_buttonEscapeMenu);

            _buttonEscapeMenu.OnToggled += args => EscapeButtonToggled?.Invoke(args.Pressed);

            // Character
            _buttonCharacterMenu = new TopButton(characterTexture, ContentKeyFunctions.OpenCharacterMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open character menu."),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonCharacterMenu);

            _buttonCharacterMenu.OnToggled += args => CharacterButtonToggled?.Invoke(args.Pressed);

            // Inventory
            _buttonInventoryMenu = new TopButton(inventoryTexture, ContentKeyFunctions.OpenInventoryMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open inventory menu."),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonInventoryMenu);

            _buttonInventoryMenu.OnToggled += args => InventoryButtonDown = args.Pressed;

            // Crafting
            _buttonCraftingMenu = new TopButton(craftingTexture, ContentKeyFunctions.OpenCraftingMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open crafting menu."),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonCraftingMenu);

            _buttonCraftingMenu.OnToggled += args => CraftingButtonToggled?.Invoke(args.Pressed);

            // Actions
            _buttonActionsMenu = new TopButton(actionsTexture, ContentKeyFunctions.OpenActionsMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open actions menu."),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonActionsMenu);

            _buttonActionsMenu.OnToggled += args => ActionsButtonToggled?.Invoke(args.Pressed);

            // Admin
            _buttonAdminMenu = new TopButton(adminTexture, ContentKeyFunctions.OpenAdminMenu, _inputManager)
            {
                ToolTip = Loc.GetString("Open admin menu."),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonAdminMenu);

            _buttonAdminMenu.OnToggled += args => AdminButtonToggled?.Invoke(args.Pressed);

            // Sandbox
            _buttonSandboxMenu = new TopButton(sandboxTexture, ContentKeyFunctions.OpenSandboxWindow, _inputManager)
            {
                ToolTip = Loc.GetString("Open sandbox menu."),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = {StyleBase.ButtonSquare}
            };

            _topButtonsContainer.AddChild(_buttonSandboxMenu);

            _buttonSandboxMenu.OnToggled += args => SandboxButtonToggled?.Invoke(args.Pressed);

            // Info Window
            _buttonInfo = new TopButton(infoTexture, ContentKeyFunctions.OpenInfo, _inputManager)
            {
                ToolTip = Loc.GetString("ui-options-function-open-info"),
                MinSize = topMinSize,
                StyleClasses = {StyleBase.ButtonOpenLeft, TopButton.StyleClassRedTopButton},
            };

            _topButtonsContainer.AddChild(_buttonInfo);

            _buttonInfo.OnToggled += a => ButtonInfoOnOnToggled();

            _infoWindow = new InfoWindow();

            _infoWindow.OnClose += () => _buttonInfo.Pressed = false;

            _inputManager.SetInputCommand(ContentKeyFunctions.OpenInfo,
                InputCmdHandler.FromDelegate(s => ButtonInfoOnOnToggled()));


            _combatPanelContainer = new VBoxContainer
            {
                HorizontalAlignment = Control.HAlignment.Left,
                VerticalAlignment = Control.VAlignment.Bottom,
                Children =
                {
                    (_targetingDoll = new TargetingDoll(_resourceCache))
                }
            };

            LC.SetGrowHorizontal(_combatPanelContainer, LC.GrowDirection.Begin);
            LC.SetGrowVertical(_combatPanelContainer, LC.GrowDirection.Begin);
            LC.SetAnchorAndMarginPreset(_combatPanelContainer, LC.LayoutPreset.BottomRight);
            LC.SetMarginBottom(_combatPanelContainer, -10f);

            _targetingDoll.OnZoneChanged += args => OnTargetingZoneChanged?.Invoke(args);

            var centerBottomContainer = new VBoxContainer
            {
                SeparationOverride = 5,
                HorizontalAlignment = Control.HAlignment.Center
            };
            LC.SetAnchorAndMarginPreset(centerBottomContainer, LC.LayoutPreset.CenterBottom);
            LC.SetGrowHorizontal(centerBottomContainer, LC.GrowDirection.Both);
            LC.SetGrowVertical(centerBottomContainer, LC.GrowDirection.Begin);
            LC.SetMarginBottom(centerBottomContainer, -10f);
            RootControl.AddChild(centerBottomContainer);

            HandsContainer = new Control
            {
                VerticalAlignment = Control.VAlignment.Bottom,
                HorizontalAlignment = Control.HAlignment.Center
            };
            BottomRightInventoryQuickButtonContainer = new HBoxContainer()
            {
                VerticalAlignment = Control.VAlignment.Bottom,
                HorizontalAlignment = Control.HAlignment.Right
            };
            BottomLeftInventoryQuickButtonContainer = new HBoxContainer()
            {
                VerticalAlignment = Control.VAlignment.Bottom,
                HorizontalAlignment = Control.HAlignment.Left
            };
            TopInventoryQuickButtonContainer = new HBoxContainer()
            {
                Visible = false,
                VerticalAlignment = Control.VAlignment.Bottom,
                HorizontalAlignment = Control.HAlignment.Center
            };
            var bottomRow = new HBoxContainer()
            {
                HorizontalAlignment = Control.HAlignment.Center

            };
            bottomRow.AddChild(new Control {MinSize = (69, 0)}); //Padding (nice)
            bottomRow.AddChild(BottomLeftInventoryQuickButtonContainer);
            bottomRow.AddChild(HandsContainer);
            bottomRow.AddChild(BottomRightInventoryQuickButtonContainer);
            bottomRow.AddChild(new Control {MinSize = (1, 0)}); //Padding


            centerBottomContainer.AddChild(TopInventoryQuickButtonContainer);
            centerBottomContainer.AddChild(bottomRow);

            SuspicionContainer = new Control
            {
                HorizontalAlignment = Control.HAlignment.Center
            };

            var rightBottomContainer = new HBoxContainer
            {
                SeparationOverride = 5
            };
            LC.SetAnchorAndMarginPreset(rightBottomContainer, LC.LayoutPreset.BottomRight);
            LC.SetGrowHorizontal(rightBottomContainer, LC.GrowDirection.Begin);
            LC.SetGrowVertical(rightBottomContainer, LC.GrowDirection.Begin);
            LC.SetMarginBottom(rightBottomContainer, -10f);
            LC.SetMarginRight(rightBottomContainer, -10f);
            RootControl.AddChild(rightBottomContainer);

            rightBottomContainer.AddChild(_combatPanelContainer);

            RootControl.AddChild(SuspicionContainer);

            LC.SetAnchorAndMarginPreset(SuspicionContainer, LC.LayoutPreset.BottomLeft,
                margin: 10);
            LC.SetGrowHorizontal(SuspicionContainer, LC.GrowDirection.End);
            LC.SetGrowVertical(SuspicionContainer, LC.GrowDirection.Begin);

            _topNotificationContainer = new VBoxContainer
            {
                MinSize = (600, 0)
            };
            RootControl.AddChild(_topNotificationContainer);
            LC.SetAnchorPreset(_topNotificationContainer, LC.LayoutPreset.CenterTop);
            LC.SetGrowHorizontal(_topNotificationContainer, LC.GrowDirection.Both);
            LC.SetGrowVertical(_topNotificationContainer, LC.GrowDirection.End);

            VoteContainer = new VBoxContainer();
            RootControl.AddChild(VoteContainer);
            LC.SetAnchorPreset(VoteContainer, LC.LayoutPreset.TopLeft);
            LC.SetMarginLeft(VoteContainer, 180);
            LC.SetMarginTop(VoteContainer, 100);
            LC.SetGrowHorizontal(VoteContainer, LC.GrowDirection.End);
            LC.SetGrowVertical(VoteContainer, LC.GrowDirection.End);
        }

        private void ButtonInfoOnOnToggled()
        {
            _buttonInfo.StyleClasses.Remove(TopButton.StyleClassRedTopButton);
            if (_infoWindow.IsOpen)
            {
                if (!_infoWindow.IsAtFront())
                {
                    _infoWindow.MoveToFront();
                    _buttonInfo.Pressed = true;
                }
                else
                {
                    _infoWindow.Close();
                    _buttonInfo.Pressed = false;
                }
            }
            else
            {
                _infoWindow.OpenCentered();
                _buttonInfo.Pressed = true;
            }
        }

        public Control RootControl { get; private set; } = default!;

        public bool EscapeButtonDown
        {
            get => _buttonEscapeMenu.Pressed;
            set => _buttonEscapeMenu.Pressed = value;
        }

        public Action<bool>? EscapeButtonToggled { get; set; }

        public bool CharacterButtonDown
        {
            get => _buttonCharacterMenu.Pressed;
            set => _buttonCharacterMenu.Pressed = value;
        }

        public bool CharacterButtonVisible
        {
            get => _buttonCharacterMenu.Visible;
            set => _buttonCharacterMenu.Visible = value;
        }

        public Action<bool>? CharacterButtonToggled { get; set; }

        public bool InventoryButtonDown
        {
            get => _buttonInventoryMenu.Pressed;
            set
            {
                TopInventoryQuickButtonContainer.Visible = value;
                _buttonInventoryMenu.Pressed = value;
            }
        }

        public bool InventoryButtonVisible
        {
            get => _buttonInventoryMenu.Visible;
            set => _buttonInventoryMenu.Visible = value;
        }

        public bool CraftingButtonDown
        {
            get => _buttonCraftingMenu.Pressed;
            set => _buttonCraftingMenu.Pressed = value;
        }

        public bool CraftingButtonVisible
        {
            get => _buttonCraftingMenu.Visible;
            set => _buttonCraftingMenu.Visible = value;
        }

        public Action<bool>? CraftingButtonToggled { get; set; }

        public bool ActionsButtonDown
        {
            get => _buttonActionsMenu.Pressed;
            set => _buttonActionsMenu.Pressed = value;
        }

        public bool ActionsButtonVisible
        {
            get => _buttonActionsMenu.Visible;
            set => _buttonActionsMenu.Visible = value;
        }

        public Action<bool>? ActionsButtonToggled { get; set; }

        public bool AdminButtonDown
        {
            get => _buttonAdminMenu.Pressed;
            set => _buttonAdminMenu.Pressed = value;
        }

        public bool AdminButtonVisible
        {
            get => _buttonAdminMenu.Visible;
            set => _buttonAdminMenu.Visible = value;
        }

        public Action<bool>? AdminButtonToggled { get; set; }

        public bool SandboxButtonDown
        {
            get => _buttonSandboxMenu.Pressed;
            set => _buttonSandboxMenu.Pressed = value;
        }

        public bool SandboxButtonVisible
        {
            get => _buttonSandboxMenu.Visible;
            set => _buttonSandboxMenu.Visible = value;
        }

        public Action<bool>? SandboxButtonToggled { get; set; }

        public Control VoteContainer { get; private set; } = default!;

        public sealed class TopButton : ContainerButton
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
                    new VBoxContainer
                    {
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
            }

            protected override void ExitedTree()
            {
                _inputManager.OnKeyBindingAdded -= OnKeyBindingChanged;
                _inputManager.OnKeyBindingRemoved -= OnKeyBindingChanged;
            }


            private void OnKeyBindingChanged(IKeyBinding obj)
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
                    if (binding.Mod1 != Unknown || binding.Mod2 != Unknown ||
                        binding.Mod3 != Unknown)
                    {
                        name = null;
                        return false;
                    }

                    name = null;
                    name = key switch
                    {
                        Apostrophe => "'",
                        Comma => ",",
                        Delete => "Del",
                        Down => "Dwn",
                        Escape => "Esc",
                        Equal => "=",
                        Home => "Hom",
                        Insert => "Ins",
                        Left => "Lft",
                        Menu => "Men",
                        Minus => "-",
                        Num0 => "0",
                        Num1 => "1",
                        Num2 => "2",
                        Num3 => "3",
                        Num4 => "4",
                        Num5 => "5",
                        Num6 => "6",
                        Num7 => "7",
                        Num8 => "8",
                        Num9 => "9",
                        Pause => "||",
                        Period => ".",
                        Return => "Ret",
                        Right => "Rgt",
                        Slash => "/",
                        Space => "Spc",
                        Tab => "Tab",
                        Tilde => "~",
                        BackSlash => "\\",
                        BackSpace => "Bks",
                        LBracket => "[",
                        MouseButton4 => "M4",
                        MouseButton5 => "M5",
                        MouseButton6 => "M6",
                        MouseButton7 => "M7",
                        MouseButton8 => "M8",
                        MouseButton9 => "M9",
                        MouseLeft => "ML",
                        MouseMiddle => "MM",
                        MouseRight => "MR",
                        NumpadDecimal => "N.",
                        NumpadDivide => "N/",
                        NumpadEnter => "Ent",
                        NumpadMultiply => "*",
                        NumpadNum0 => "0",
                        NumpadNum1 => "1",
                        NumpadNum2 => "2",
                        NumpadNum3 => "3",
                        NumpadNum4 => "4",
                        NumpadNum5 => "5",
                        NumpadNum6 => "6",
                        NumpadNum7 => "7",
                        NumpadNum8 => "8",
                        NumpadNum9 => "9",
                        NumpadSubtract => "N-",
                        PageDown => "PgD",
                        PageUp => "PgU",
                        RBracket => "]",
                        SemiColon => ";",
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
    }
}
