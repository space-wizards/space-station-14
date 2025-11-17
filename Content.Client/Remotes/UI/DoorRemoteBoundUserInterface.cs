using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;
using Robust.Client.UserInterface;

namespace Content.Client.Remotes.UI;

public sealed class DoorRemoteBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly Color SelectedOptionColor = Palettes.Green.Element.WithAlpha(128);
    private static readonly Color SelectedOptionHoverColor = Palettes.Green.HoveredElement.WithAlpha(128);

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

    private IEnumerable<RadialMenuOptionBase> CreateButtons(OperatingMode selectedMode, List<DoorRemoteModeInfo> modeOptions)
    {
        var options = new List<RadialMenuOptionBase>();
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

            var option = new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, modeOption.Mode)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(modeOption.Icon),
                ToolTip = Loc.GetString(modeOption.Tooltip),
                BackgroundColor = optionCustomColor,
                HoverBackgroundColor = optionHoverCustomColor
            };
            options.Add(option);
        }

        return options;
    }

    private void HandleRadialMenuClick(OperatingMode mode)
    {
        var msg = new DoorRemoteModeChangeMessage { Mode = mode };
        SendPredictedMessage(msg);
    }
}
