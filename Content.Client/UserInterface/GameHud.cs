using System;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

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
        Action<bool> EscapeButtonToggled { get; set; }

        // Character top button.
        bool CharacterButtonDown { get; set; }
        bool CharacterButtonVisible { get; set; }
        Action<bool> CharacterButtonToggled { get; set; }

        // Inventory top button.
        bool InventoryButtonDown { get; set; }
        bool InventoryButtonVisible { get; set; }
        Action<bool> InventoryButtonToggled { get; set; }

        // Crafting top button.
        bool CraftingButtonDown { get; set; }
        bool CraftingButtonVisible { get; set; }
        Action<bool> CraftingButtonToggled { get; set; }

        // Sandbox top button.
        bool SandboxButtonDown { get; set; }
        bool SandboxButtonVisible { get; set; }
        Action<bool> SandboxButtonToggled { get; set; }

        Control HandsContainer { get; }
        Control SuspicionContainer { get; }
        Control InventoryQuickButtonContainer { get; }

        bool CombatPanelVisible { get; set; }
        bool CombatModeActive { get; set; }
        TargetingZone TargetingZone { get; set; }
        Action<bool> OnCombatModeChanged { get; set; }
        Action<TargetingZone> OnTargetingZoneChanged { get; set; }


        // Init logic.
        void Initialize();
    }

    internal sealed class GameHud : IGameHud
    {
        private HBoxContainer _topButtonsContainer;
        private TopButton _buttonEscapeMenu;
        private TopButton _buttonTutorial;
        private TopButton _buttonCharacterMenu;
        private TopButton _buttonInventoryMenu;
        private TopButton _buttonCraftingMenu;
        private TopButton _buttonSandboxMenu;
        private TutorialWindow _tutorialWindow;
        private TargetingDoll _targetingDoll;
        private Button _combatModeButton;
        private VBoxContainer _combatPanelContainer;

        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        public Control HandsContainer { get; private set; }
        public Control SuspicionContainer { get; private set; }
        public Control InventoryQuickButtonContainer { get; private set; }

        public bool CombatPanelVisible
        {
            get => _combatPanelContainer.Visible;
            set => _combatPanelContainer.Visible = value;
        }

        public bool CombatModeActive
        {
            get => _combatModeButton.Pressed;
            set => _combatModeButton.Pressed = value;
        }

        public TargetingZone TargetingZone
        {
            get => _targetingDoll.ActiveZone;
            set => _targetingDoll.ActiveZone = value;
        }

        public Action<bool> OnCombatModeChanged { get; set; }
        public Action<TargetingZone> OnTargetingZoneChanged { get; set; }

        public void Initialize()
        {
            RootControl = new LayoutContainer();
            LayoutContainer.SetAnchorPreset(RootControl, LayoutContainer.LayoutPreset.Wide);

            var escapeTexture = _resourceCache.GetTexture("/Textures/Interface/hamburger.svg.96dpi.png");
            var characterTexture = _resourceCache.GetTexture("/Textures/Interface/character.svg.96dpi.png");
            var inventoryTexture = _resourceCache.GetTexture("/Textures/Interface/inventory.svg.96dpi.png");
            var craftingTexture = _resourceCache.GetTexture("/Textures/Interface/hammer.svg.96dpi.png");
            var tutorialTexture = _resourceCache.GetTexture("/Textures/Interface/students-cap.svg.96dpi.png");
            var sandboxTexture = _resourceCache.GetTexture("/Textures/Interface/sandbox.svg.96dpi.png");

            _topButtonsContainer = new HBoxContainer
            {
                SeparationOverride = 4
            };

            RootControl.AddChild(_topButtonsContainer);

            LayoutContainer.SetAnchorAndMarginPreset(_topButtonsContainer, LayoutContainer.LayoutPreset.TopLeft,
                margin: 10);

            // TODO: Pull key names here from the actual key binding config.
            // Escape
            _buttonEscapeMenu = new TopButton(escapeTexture, "Esc")
            {
                ToolTip = Loc.GetString("Open escape menu.")
            };

            _topButtonsContainer.AddChild(_buttonEscapeMenu);

            _buttonEscapeMenu.OnToggled += args => EscapeButtonToggled?.Invoke(args.Pressed);

            // Tutorial
            _buttonTutorial = new TopButton(tutorialTexture, "F1")
            {
                ToolTip = Loc.GetString("Open tutorial.")
            };

            _topButtonsContainer.AddChild(_buttonTutorial);

            _buttonTutorial.OnToggled += a => ButtonTutorialOnOnToggled();

            // Character
            _buttonCharacterMenu = new TopButton(characterTexture, "C")
            {
                ToolTip = Loc.GetString("Open character menu."),
                Visible = false
            };

            _topButtonsContainer.AddChild(_buttonCharacterMenu);

            _buttonCharacterMenu.OnToggled += args => CharacterButtonToggled?.Invoke(args.Pressed);

            // Inventory
            _buttonInventoryMenu = new TopButton(inventoryTexture, "I")
            {
                ToolTip = Loc.GetString("Open inventory menu."),
                Visible = false
            };

            _topButtonsContainer.AddChild(_buttonInventoryMenu);

            _buttonInventoryMenu.OnToggled += args => InventoryButtonToggled?.Invoke(args.Pressed);

            // Crafting
            _buttonCraftingMenu = new TopButton(craftingTexture, "G")
            {
                ToolTip = Loc.GetString("Open crafting menu."),
                Visible = false
            };

            _topButtonsContainer.AddChild(_buttonCraftingMenu);

            _buttonCraftingMenu.OnToggled += args => CraftingButtonToggled?.Invoke(args.Pressed);

            // Sandbox
            _buttonSandboxMenu = new TopButton(sandboxTexture, "B")
            {
                ToolTip = Loc.GetString("Open sandbox menu."),
                Visible = false
            };

            _topButtonsContainer.AddChild(_buttonSandboxMenu);

            _buttonSandboxMenu.OnToggled += args => SandboxButtonToggled?.Invoke(args.Pressed);

            _tutorialWindow = new TutorialWindow();

            _tutorialWindow.OnClose += () => _buttonTutorial.Pressed = false;

            _inputManager.SetInputCommand(ContentKeyFunctions.OpenTutorial,
                InputCmdHandler.FromDelegate(s => ButtonTutorialOnOnToggled()));

            var inventoryContainer = new HBoxContainer
            {
                SeparationOverride = 10
            };

            RootControl.AddChild(inventoryContainer);

            LayoutContainer.SetGrowHorizontal(inventoryContainer, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetGrowVertical(inventoryContainer, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetAnchorAndMarginPreset(inventoryContainer, LayoutContainer.LayoutPreset.BottomRight);

            InventoryQuickButtonContainer = new MarginContainer
            {
                SizeFlagsVertical = Control.SizeFlags.ShrinkEnd
            };

            _combatPanelContainer = new VBoxContainer
            {
                Children =
                {
                    (_combatModeButton = new Button
                    {
                        Text = Loc.GetString("Combat Mode"),
                        ToggleMode = true
                    }),
                    (_targetingDoll = new TargetingDoll(_resourceCache))
                }
            };

            _combatModeButton.OnToggled += args => OnCombatModeChanged?.Invoke(args.Pressed);
            _targetingDoll.OnZoneChanged += args => OnTargetingZoneChanged?.Invoke(args);

            inventoryContainer.Children.Add(InventoryQuickButtonContainer);
            inventoryContainer.Children.Add(_combatPanelContainer);


            HandsContainer = new MarginContainer
            {
                SizeFlagsVertical = Control.SizeFlags.ShrinkEnd
            };

            RootControl.AddChild(HandsContainer);

            LayoutContainer.SetAnchorAndMarginPreset(HandsContainer, LayoutContainer.LayoutPreset.CenterBottom);
            LayoutContainer.SetGrowHorizontal(HandsContainer, LayoutContainer.GrowDirection.Both);
            LayoutContainer.SetGrowVertical(HandsContainer, LayoutContainer.GrowDirection.Begin);

            SuspicionContainer = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
            };

            RootControl.AddChild(SuspicionContainer);

            LayoutContainer.SetAnchorAndMarginPreset(SuspicionContainer, LayoutContainer.LayoutPreset.BottomLeft, margin: 10);
            LayoutContainer.SetGrowHorizontal(SuspicionContainer, LayoutContainer.GrowDirection.End);
            LayoutContainer.SetGrowVertical(SuspicionContainer, LayoutContainer.GrowDirection.Begin);
        }

        private void ButtonTutorialOnOnToggled()
        {
            if (_tutorialWindow.IsOpen)
            {
                if (!_tutorialWindow.IsAtFront())
                {
                    _tutorialWindow.MoveToFront();
                    _buttonTutorial.Pressed = true;
                }
                else
                {
                    _tutorialWindow.Close();
                    _buttonTutorial.Pressed = false;
                }
            }
            else
            {
                _tutorialWindow.OpenCentered();
                _buttonTutorial.Pressed = true;
            }
        }

        public Control RootControl { get; private set; }

        public bool EscapeButtonDown
        {
            get => _buttonEscapeMenu.Pressed;
            set => _buttonEscapeMenu.Pressed = value;
        }

        public Action<bool> EscapeButtonToggled { get; set; }

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

        public Action<bool> CharacterButtonToggled { get; set; }

        public bool InventoryButtonDown
        {
            get => _buttonInventoryMenu.Pressed;
            set => _buttonInventoryMenu.Pressed = value;
        }

        public bool InventoryButtonVisible
        {
            get => _buttonInventoryMenu.Visible;
            set => _buttonInventoryMenu.Visible = value;
        }

        public Action<bool> InventoryButtonToggled { get; set; }

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

        public Action<bool> CraftingButtonToggled { get; set; }

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

        public Action<bool> SandboxButtonToggled { get; set; }

        public sealed class TopButton : BaseButton
        {
            public const string StyleClassLabelTopButton = "topButtonLabel";

            private static readonly Color ColorNormal = Color.FromHex("#7b7e9e");
            private static readonly Color ColorHovered = Color.FromHex("#9699bb");
            private static readonly Color ColorPressed = Color.FromHex("#789B8C");

            private readonly TextureRect _textureRect;
            private readonly Label _label;

            public TopButton(Texture texture, string keyName)
            {
                ToggleMode = true;

                AddChild(new MarginContainer
                {
                    MarginTopOverride = 4,
                    Children =
                    {
                        new VBoxContainer
                        {
                            Children =
                            {
                                (_textureRect = new TextureRect
                                {
                                    Texture = texture,
                                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                                    SizeFlagsVertical = SizeFlags.Expand | SizeFlags.ShrinkCenter,
                                    ModulateSelfOverride = ColorNormal,
                                    CustomMinimumSize = (0, 32),
                                    Stretch = TextureRect.StretchMode.KeepCentered
                                }),
                                (_label = new Label
                                {
                                    Text = keyName,
                                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                                    ModulateSelfOverride = ColorNormal,
                                    StyleClasses = {StyleClassLabelTopButton}
                                })
                            }
                        }
                    }
                });

                DrawModeChanged();
            }

            protected override Vector2 CalculateMinimumSize()
            {
                var styleSize = ActualStyleBox?.MinimumSize ?? Vector2.Zero;
                return (0, 4) + styleSize + base.CalculateMinimumSize();
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                ActualStyleBox?.Draw(handle, PixelSizeBox);
            }

            private StyleBox ActualStyleBox
            {
                get
                {
                    TryGetStyleProperty(Button.StylePropertyStyleBox, out StyleBox ret);
                    return ret;
                }
            }

            protected override void DrawModeChanged()
            {
                switch (DrawMode)
                {
                    case DrawModeEnum.Normal:
                        SetOnlyStylePseudoClass(Button.StylePseudoClassNormal);
                        _textureRect.ModulateSelfOverride = ColorNormal;
                        _label.ModulateSelfOverride = ColorNormal;
                        break;

                    case DrawModeEnum.Pressed:
                        SetOnlyStylePseudoClass(Button.StylePseudoClassPressed);
                        _textureRect.ModulateSelfOverride = ColorPressed;
                        _label.ModulateSelfOverride = ColorPressed;
                        break;

                    case DrawModeEnum.Hover:
                        SetOnlyStylePseudoClass(Button.StylePseudoClassHover);
                        _textureRect.ModulateSelfOverride = ColorHovered;
                        _label.ModulateSelfOverride = ColorHovered;
                        break;

                    case DrawModeEnum.Disabled:
                        break;
                }
            }

            protected override void LayoutUpdateOverride()
            {
                var box = ActualStyleBox ?? new StyleBoxEmpty();
                var contentBox = box.GetContentBox(PixelSizeBox);

                foreach (var child in Children)
                {
                    FitChildInPixelBox(child, (UIBox2i) contentBox);
                }
            }
        }
    }
}
