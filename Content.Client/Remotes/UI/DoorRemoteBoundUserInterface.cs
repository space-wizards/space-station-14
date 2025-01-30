using Content.Client.UserInterface.Controls;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Remotes.UI;

public class DoorRemoteBoundUserInterface: BoundUserInterface
{
    private RadialMenu? _menu;

    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IGameTiming _timing;

    /// <inheritdoc />
    public DoorRemoteBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!EntMan.HasComponent<DoorRemoteComponent>(Owner))
            return;

        if (_menu is { IsOpen: true })
        {
            _menu.Close();
            return;
        }

        var models = CreateButtons();

        _menu = new SimpleRadialMenu(models);
        _menu.OnClose += Close;

        // Open the menu, centered on the mouse
        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }


    private IEnumerable<RadialMenuOption> CreateButtons()
    {
        return new[]
        {
            new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, OperatingMode.OpenClose)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "unbolt_door"),
                ToolTip = "open/close"
            },
            new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, OperatingMode.ToggleBolts)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "bolt_door"),
                ToolTip = "bolt"
            },
            new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, OperatingMode.ToggleEmergencyAccess)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "emergency_on"),
                ToolTip = "emergency access"
            },
        };
    }

    private void HandleRadialMenuClick(OperatingMode mode)
    {
        SendPredictedMessage(new DoorRemoteModeChangeMessage { Mode= mode });
    }
}
