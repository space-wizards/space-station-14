using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Weapons.Ranged.UI;

/// <summary>
/// BUI for simple radial that helps to change battery-weapons fire mode.
/// </summary>
public sealed class BatteryWeaponFireModesBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SimpleRadialMenu? _menu;

    private static readonly Color SelectedModeBackgroundColor = StyleNano.ButtonColorGoodDefault.WithAlpha(128);
    private static readonly Color SelectedModeHoverBackgroundColor = StyleNano.ButtonColorGoodHovered.WithAlpha(128);

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out BatteryWeaponFireModesComponent? fireModes))
            return;

        var models = CreateButtons(fireModes);
        if (models.Count <= 1)
            Close();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    /// <summary>
    /// Collect options for radial menu from component's <see cref="BatteryWeaponFireModesComponent.FireModes"/>.
    /// </summary>
    private List<RadialMenuOptionBase> CreateButtons(BatteryWeaponFireModesComponent fireModes)
    {
        var list = new List<RadialMenuOptionBase>();

        for (var index = 0; index < fireModes.FireModes.Count; index++)
        {
            var fireMode = fireModes.FireModes[index];
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);
            var option = new RadialMenuActionOption<int>(HandleRadialMenuClick, index)
            {
                ToolTip = entProto.Name,
                IconSpecifier = RadialMenuIconSpecifier.With(fireMode.ModeIcon)
            };
            if (index == fireModes.CurrentFireMode)
            {
                option.BackgroundColor = SelectedModeBackgroundColor;
                option.HoverBackgroundColor = SelectedModeHoverBackgroundColor;
            }

            list.Add(option);
        }

        return list;
    }

    private void HandleRadialMenuClick(int modeIndex)
    {
        var msg = new BatteryWeaponFireModeChangeMessage { ModeIndex = modeIndex };
        SendPredictedMessage(msg);
    }
}
