using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Weapons.Ranged.UI;

/// <summary>
/// BUI for simple radial that helps to change battery-weapons fire mode.
/// </summary>
public sealed class BatteryWeaponFireModesBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SimpleRadialMenu? _menu;

    private readonly Color _selectedModeBackgroundColor = StyleNano.ButtonColorGoodDefault.WithAlpha(128);
    private readonly Color _selectedModeHoverBackgroundColor = StyleNano.ButtonColorGoodHovered.WithAlpha(128);

    /// <inheritdoc />
    public BatteryWeaponFireModesBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out BatteryWeaponFireModesComponent? fireModes))
            return;

        var models = CreateButtons(fireModes);
        if (models.Count<= 1)
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

        for (var i = 0; i < fireModes.FireModes.Count; i++)
        {
            var fireMode = fireModes.FireModes[i];
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);
            var index = i;
            var option = new RadialMenuActionOption<BatteryWeaponFireMode>(mode => HandleRadialMenuClick(index), fireMode)
            {
                ToolTip = entProto.Name,
                IconSpecifier = RadialMenuIconSpecifier.With(fireMode.ModeIcon)
            };
            if (index == fireModes.CurrentFireMode)
            {
                option.BackgroundColor = _selectedModeBackgroundColor;
                option.HoverBackgroundColor = _selectedModeHoverBackgroundColor;
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
