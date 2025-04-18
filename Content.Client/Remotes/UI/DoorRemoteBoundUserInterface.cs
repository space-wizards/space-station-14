using Content.Client.UserInterface.Controls;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;
using Robust.Client.UserInterface;

namespace Content.Client.Remotes.UI;

public sealed class DoorRemoteBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly Color SelectedOptionColor = new Color(62, 108, 69, 128);
    private static readonly Color SelectedOptionHoverColor = new Color(82, 128, 89, 128);

    private SimpleRadialMenu? _menu;
    
    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<DoorRemoteComponent>(Owner, out var remote))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        var models = CreateButtons(remote.Mode, remote.Options);
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    private RadialMenuOption[] CreateButtons(OperatingMode selectedMode, List<DoorRemoteModeInfo> modeOptions)
    {
        var options = new RadialMenuOption[modeOptions.Count];
        for (var i = 0; i < modeOptions.Count; i++)
        {
            var modeOption = modeOptions[i];

            Color? optionCustomColor = null;
            Color? optionHoverCustomColor = null;
            if (modeOption.Mode == selectedMode)
            {
                optionCustomColor = SelectedOptionColor;
                optionHoverCustomColor = SelectedOptionHoverColor;
            }

            options[i] = new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, modeOption.Mode)
            {
                Sprite = modeOption.Icon,
                ToolTip = Loc.GetString(modeOption.Tooltip),
                BackgroundColor = optionCustomColor,
                HoverBackgroundColor = optionHoverCustomColor
            };
        }

        return options;
    }

    private void HandleRadialMenuClick(OperatingMode mode)
    {
        var msg = new DoorRemoteModeChangeMessage { Mode = mode };
        SendPredictedMessage(msg);
    }
}
