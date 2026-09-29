using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiDoorRemoteBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly Color SelectedOptionColor = Palettes.Green.Element.WithAlpha(128);
    private static readonly Color SelectedOptionHoverColor = Palettes.Green.HoveredElement.WithAlpha(128);

    private static readonly DoorRemoteModeInfo[] ModeOptions =
    {
        new()
        {
            Mode = OperatingMode.OpenClose,
            Tooltip = "door-remote-open-close-text",
            Icon = new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Structures/Doors/Airlocks/Standard/airlock-base.rsi"),
                "assembly"),
        },
        new()
        {
            Mode = OperatingMode.ToggleBolts,
            Tooltip = "door-remote-toggle-bolt-text",
            Icon = new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Actions/actions_ai.rsi"),
                "bolt_door"),
        },
        new()
        {
            Mode = OperatingMode.ToggleEmergencyAccess,
            Tooltip = "door-remote-emergency-access-text",
            Icon = new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Actions/actions_ai.rsi"),
                "emergency_on"),
        },
        new()
        {
            Mode = OperatingMode.ToggleOvercharge,
            Tooltip = "door-remote-toggle-eletrify-text",
            Icon = new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Actions/actions_ai.rsi"),
                "door_overcharge_on"),
        },
    };

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<DoorRemoteComponent>(Owner, out var remote))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(CreateButtons(remote.Mode));
        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> CreateButtons(OperatingMode selectedMode)
    {
        var options = new List<RadialMenuOptionBase>(ModeOptions.Length);
        for (var i = 0; i < ModeOptions.Length; i++)
        {
            var modeOption = ModeOptions[i];
            Color? optionCustomColor = null;
            Color? optionHoverCustomColor = null;

            if (modeOption.Mode == selectedMode)
            {
                optionCustomColor = SelectedOptionColor;
                optionHoverCustomColor = SelectedOptionHoverColor;
            }

            options.Add(new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, modeOption.Mode)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(modeOption.Icon),
                ToolTip = Loc.GetString(modeOption.Tooltip),
                BackgroundColor = optionCustomColor,
                HoverBackgroundColor = optionHoverCustomColor,
                Order = i,
            });
        }

        return options;
    }

    private void HandleRadialMenuClick(OperatingMode mode)
    {
        SendPredictedMessage(new DoorRemoteModeChangeMessage { Mode = mode });
    }
}
