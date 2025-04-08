using Content.Client.UserInterface.Controls;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Remotes.UI;

public sealed class DoorRemoteBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

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

    private RadialMenuOption[] CreateButtons()
    {
        return new[]
        {
            new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, OperatingMode.OpenClose)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Doors/Airlocks/Standard/basic.rsi"), "assembly"),
                ToolTip = Loc.GetString("door-remote-open-close-text")
            },
            new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, OperatingMode.ToggleBolts)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "bolt_door"),
                ToolTip = Loc.GetString("door-remote-toggle-bolt-text")
            },
            new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, OperatingMode.ToggleEmergencyAccess)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "emergency_on"),
                ToolTip = Loc.GetString("door-remote-emergency-access-text")
            },
        };
    }

    private void HandleRadialMenuClick(OperatingMode mode)
    {
        SendPredictedMessage(new DoorRemoteModeChangeMessage { Mode= mode });
    }
}
