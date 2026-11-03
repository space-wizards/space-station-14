using Content.Client.UserInterface.Controls;
using Content.Client.Weapons.Ranged.UI;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Client.Singularity.UI;

/// <summary>
/// BUI for simple radial that helps to interact with emitter settings.
/// </summary>
public sealed partial class PowerToggleableChangeModeBoundUserInterface(EntityUid owner, Enum uiKey)
    : BatteryWeaponFireModesBoundUserInterface(owner, uiKey)
{
    private static readonly SpriteSpecifier.Texture TogglePowerTexture = new(new("/Textures/Interface/Radial/toggle-power.png"));

    protected override List<RadialMenuOptionBase> CreateButtons(BatteryWeaponFireModesComponent fireModes)
    {
        var options = base.CreateButtons(fireModes);
        if (!EntMan.TryGetComponent(Owner, out PowerStateComponent? emitter))
            return options;

        if (options.Count == 1)
        {
            HandleSendToggle();
            return []; // returning empty will auto-close
        }

        RadialMenuActionOption<int> option;
        if (emitter.IsTurnedOn)
        {
            option = new RadialMenuActionOption<int>(_ => HandleSendToggle(), 0)
            {
                ToolTip = Loc.GetString("power-state-turn-off"),
                IconSpecifier = RadialMenuIconSpecifier.With(TogglePowerTexture),
                BackgroundColor = SelectedModeBackgroundColor,
                HoverBackgroundColor = SelectedModeHoverBackgroundColor,
            };
        }
        else
        {
            option = new RadialMenuActionOption<int>(_ => HandleSendToggle(), 0)
            {
                ToolTip = Loc.GetString("power-state-turn-on"),
                IconSpecifier = RadialMenuIconSpecifier.With(TogglePowerTexture),
            };
        }

        options.Add(option);
        return options;
    }

    private void HandleSendToggle()
    {
        var msg = new PowerStateToggleActiveMessage();
        SendPredictedMessage(msg);
    }
}
