using Content.Client.UserInterface.Controls;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Remotes.UI;

public class DoorRemoteBoundUserInterface: BoundUserInterface
{
    private SimpleRadialMenu? _menu;

    /// <inheritdoc />
    public DoorRemoteBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.HasComponent<DoorRemoteComponent>(Owner))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        var models = CreateButtons();
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
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
