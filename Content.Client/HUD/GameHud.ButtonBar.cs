using System;
using Content.Client.HUD.UI;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.HUD;

public interface IButtonBarView
{
    // Escape top button.
    bool EscapeButtonDown { get; set; }
    event Action<bool> EscapeButtonToggled;

    // Character top button.
    bool CharacterButtonDown { get; set; }
    bool CharacterButtonVisible { get; set; }
    event Action<bool> CharacterButtonToggled;

    // Inventory top button.
    bool InventoryButtonDown { get; set; }
    bool InventoryButtonVisible { get; set; }
    event Action<bool> InventoryButtonToggled;

    // Crafting top button.
    bool CraftingButtonDown { get; set; }
    bool CraftingButtonVisible { get; set; }
    event Action<bool> CraftingButtonToggled;

    // Actions top button.
    bool ActionsButtonDown { get; set; }
    bool ActionsButtonVisible { get; set; }
    event Action<bool> ActionsButtonToggled;

    // Admin top button.
    bool AdminButtonDown { get; set; }
    bool AdminButtonVisible { get; set; }
    event Action<bool> AdminButtonToggled;

    // Sandbox top button.
    bool SandboxButtonDown { get; set; }
    bool SandboxButtonVisible { get; set; }
    event Action<bool> SandboxButtonToggled;

    // Info top button
    bool InfoButtonDown { get; set; }
    event Action<bool> InfoButtonToggled;
}

internal sealed partial class GameHud
{
    private TopButton _buttonEscapeMenu = default!;
    private TopButton _buttonInfo = default!;
    private TopButton _buttonCharacterMenu = default!;
    private TopButton _buttonInventoryMenu = default!;
    private TopButton _buttonCraftingMenu = default!;
    private TopButton _buttonActionsMenu = default!;
    private TopButton _buttonAdminMenu = default!;
    private TopButton _buttonSandboxMenu = default!;

    private BoxContainer GenerateButtonBar(IResourceCache resourceCache, IInputManager inputManager)
    {
        var topButtonsContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8
        };

        LayoutContainer.SetAnchorAndMarginPreset(topButtonsContainer, LayoutContainer.LayoutPreset.TopLeft, margin: 10);

        // the icon textures here should all have the same image height (32) but different widths, so in order to ensure
        // the buttons themselves are consistent widths we set a common custom min size
        Vector2 topMinSize = (42, 64);

        // Escape
        {
            _buttonEscapeMenu = new TopButton(resourceCache.GetTexture("/Textures/Interface/hamburger.svg.192dpi.png"),
                EngineKeyFunctions.EscapeMenu, inputManager)
            {
                ToolTip = Loc.GetString("game-hud-open-escape-menu-button-tooltip"),
                MinSize = (70, 64),
                StyleClasses = { StyleBase.ButtonOpenRight }
            };

            topButtonsContainer.AddChild(_buttonEscapeMenu);

            _buttonEscapeMenu.OnToggled += args => EscapeButtonToggled?.Invoke(args.Pressed);
        }

        // Character
        {
            _buttonCharacterMenu = new TopButton(resourceCache.GetTexture("/Textures/Interface/character.svg.192dpi.png"),
                ContentKeyFunctions.OpenCharacterMenu, inputManager)
            {
                ToolTip = Loc.GetString("game-hud-open-character-menu-button-tooltip"),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = { StyleBase.ButtonSquare }
            };

            topButtonsContainer.AddChild(_buttonCharacterMenu);

            _buttonCharacterMenu.OnToggled += args => CharacterButtonToggled?.Invoke(args.Pressed);
        }

        // Inventory
        {
            _buttonInventoryMenu = new TopButton(resourceCache.GetTexture("/Textures/Interface/inventory.svg.192dpi.png"),
                ContentKeyFunctions.OpenInventoryMenu, inputManager)
            {
                ToolTip = Loc.GetString("game-hud-open-inventory-menu-button-tooltip"),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = { StyleBase.ButtonSquare }
            };

            topButtonsContainer.AddChild(_buttonInventoryMenu);

            _buttonInventoryMenu.OnToggled += args => InventoryButtonToggled?.Invoke(args.Pressed);
        }

        // Crafting
        {
            _buttonCraftingMenu = new TopButton(resourceCache.GetTexture("/Textures/Interface/hammer.svg.192dpi.png"),
                ContentKeyFunctions.OpenCraftingMenu, inputManager)
            {
                ToolTip = Loc.GetString("game-hud-open-crafting-menu-button-tooltip"),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = { StyleBase.ButtonSquare }
            };

            topButtonsContainer.AddChild(_buttonCraftingMenu);

            _buttonCraftingMenu.OnToggled += args => CraftingButtonToggled?.Invoke(args.Pressed);
        }

        // Actions
        {
            _buttonActionsMenu = new TopButton(resourceCache.GetTexture("/Textures/Interface/fist.svg.192dpi.png"),
                ContentKeyFunctions.OpenActionsMenu, inputManager)
            {
                ToolTip = Loc.GetString("game-hud-open-actions-menu-button-tooltip"),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = { StyleBase.ButtonSquare }
            };

            topButtonsContainer.AddChild(_buttonActionsMenu);

            _buttonActionsMenu.OnToggled += args => ActionsButtonToggled?.Invoke(args.Pressed);
        }

        // Admin
        {
            _buttonAdminMenu = new TopButton(resourceCache.GetTexture("/Textures/Interface/gavel.svg.192dpi.png"),
                ContentKeyFunctions.OpenAdminMenu, inputManager)
            {
                ToolTip = Loc.GetString("game-hud-open-admin-menu-button-tooltip"),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = { StyleBase.ButtonSquare }
            };

            topButtonsContainer.AddChild(_buttonAdminMenu);

            _buttonAdminMenu.OnToggled += args => AdminButtonToggled?.Invoke(args.Pressed);
        }

        // Sandbox
        {
            _buttonSandboxMenu = new TopButton(resourceCache.GetTexture("/Textures/Interface/sandbox.svg.192dpi.png"),
                ContentKeyFunctions.OpenSandboxWindow, inputManager)
            {
                ToolTip = Loc.GetString("game-hud-open-sandbox-menu-button-tooltip"),
                MinSize = topMinSize,
                Visible = false,
                StyleClasses = { StyleBase.ButtonSquare }
            };

            topButtonsContainer.AddChild(_buttonSandboxMenu);

            _buttonSandboxMenu.OnToggled += args => SandboxButtonToggled?.Invoke(args.Pressed);
        }

        // Info Window
        {
            _buttonInfo = new TopButton(resourceCache.GetTexture("/Textures/Interface/info.svg.192dpi.png"),
                ContentKeyFunctions.OpenInfo, inputManager)
            {
                ToolTip = Loc.GetString("ui-options-function-open-info"),
                MinSize = topMinSize,
                StyleClasses = { StyleBase.ButtonOpenLeft, TopButton.StyleClassRedTopButton },
            };

            topButtonsContainer.AddChild(_buttonInfo);

            _buttonInfo.OnToggled += args => InfoButtonToggled?.Invoke(args.Pressed);
            _buttonInfo.OnToggled += ButtonInfoToggledHandler;
        }

        return topButtonsContainer;
    }

    private void ButtonInfoToggledHandler(BaseButton.ButtonToggledEventArgs obj)
    {
        ButtonInfoToggled(obj.Pressed);
    }

    private void ButtonInfoToggled(bool pressed)
    {
        if(!pressed)
            return;

        _buttonInfo.StyleClasses.Remove(TopButton.StyleClassRedTopButton);
        _buttonInfo.OnToggled -= ButtonInfoToggledHandler;
    }

    /// <inheritdoc />
    public bool EscapeButtonDown
    {
        get => _buttonEscapeMenu.Pressed;
        set => _buttonEscapeMenu.Pressed = value;
    }

    /// <inheritdoc />
    public event Action<bool>? EscapeButtonToggled;

    /// <inheritdoc />
    public bool CharacterButtonDown
    {
        get => _buttonCharacterMenu.Pressed;
        set => _buttonCharacterMenu.Pressed = value;
    }

    /// <inheritdoc />
    public bool CharacterButtonVisible
    {
        get => _buttonCharacterMenu.Visible;
        set => _buttonCharacterMenu.Visible = value;
    }

    /// <inheritdoc />
    public event Action<bool>? CharacterButtonToggled;

    /// <inheritdoc />
    public bool InventoryButtonDown
    {
        get => _buttonInventoryMenu.Pressed;
        set => _buttonInventoryMenu.Pressed = value;
    }

    /// <inheritdoc />
    public bool InventoryButtonVisible
    {
        get => _buttonInventoryMenu.Visible;
        set => _buttonInventoryMenu.Visible = value;
    }

    /// <inheritdoc />
    public event Action<bool>? InventoryButtonToggled;

    /// <inheritdoc />
    public bool CraftingButtonDown
    {
        get => _buttonCraftingMenu.Pressed;
        set => _buttonCraftingMenu.Pressed = value;
    }

    /// <inheritdoc />
    public bool CraftingButtonVisible
    {
        get => _buttonCraftingMenu.Visible;
        set => _buttonCraftingMenu.Visible = value;
    }

    /// <inheritdoc />
    public event Action<bool>? CraftingButtonToggled;

    /// <inheritdoc />
    public bool ActionsButtonDown
    {
        get => _buttonActionsMenu.Pressed;
        set => _buttonActionsMenu.Pressed = value;
    }

    /// <inheritdoc />
    public bool ActionsButtonVisible
    {
        get => _buttonActionsMenu.Visible;
        set => _buttonActionsMenu.Visible = value;
    }

    /// <inheritdoc />
    public event Action<bool>? ActionsButtonToggled;

    /// <inheritdoc />
    public bool AdminButtonDown
    {
        get => _buttonAdminMenu.Pressed;
        set => _buttonAdminMenu.Pressed = value;
    }

    /// <inheritdoc />
    public bool AdminButtonVisible
    {
        get => _buttonAdminMenu.Visible;
        set => _buttonAdminMenu.Visible = value;
    }

    /// <inheritdoc />
    public event Action<bool>? AdminButtonToggled;

    /// <inheritdoc />
    public bool SandboxButtonDown
    {
        get => _buttonSandboxMenu.Pressed;
        set => _buttonSandboxMenu.Pressed = value;
    }

    /// <inheritdoc />
    public bool SandboxButtonVisible
    {
        get => _buttonSandboxMenu.Visible;
        set => _buttonSandboxMenu.Visible = value;
    }

    /// <inheritdoc />
    public event Action<bool>? SandboxButtonToggled;

    /// <inheritdoc />
    public bool InfoButtonDown
    {
        get => _buttonInfo.Pressed;
        set
        {
            _buttonInfo.Pressed = value;
            ButtonInfoToggled(value);
        }
    }

    /// <inheritdoc />
    public event Action<bool>? InfoButtonToggled;
}
