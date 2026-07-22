using Content.Client.UserInterface.Controls;
using Content.Client.Weapons.Ranged.UI;
using Content.Shared.Singularity.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Utility;

namespace Content.Client.Singularity.UI;

/// <summary>
/// BUI for simple radial that helps to interact with emitter settings.
/// </summary>
public sealed partial class EmitterChangeModeBoundUserInterface(EntityUid owner, Enum uiKey)
    : BatteryWeaponFireModesBoundUserInterface(owner, uiKey)
{
    private static readonly SpriteSpecifier.Texture TurnOnTexture = new(new("/Textures/Interface/Radial/turn-on.png"));
    private static readonly SpriteSpecifier.Texture TurnOffTexture = new(new("/Textures/Interface/Radial/turn-off.png"));
    protected override List<RadialMenuOptionBase> CreateButtons(BatteryWeaponFireModesComponent fireModes)
    {
        var options = base.CreateButtons(fireModes);
        if (!EntMan.TryGetComponent(Owner, out EmitterComponent? emitter))
            return options;

        RadialMenuActionOption<int> option;
        if (emitter.IsOn)
        {
            option = new RadialMenuActionOption<int>(_ => HandleSendToggle(), 0)
            {
                ToolTip = Loc.GetString("emitter-turn-off"),
                IconSpecifier = RadialMenuIconSpecifier.With(TurnOffTexture),
                BackgroundColor = SelectedModeBackgroundColor,
                HoverBackgroundColor = SelectedModeHoverBackgroundColor,
            };
        }
        else
        {
            option = new RadialMenuActionOption<int>(_ => HandleSendToggle(), 0)
            {
                ToolTip = Loc.GetString("emitter-turn-on"),
                IconSpecifier = RadialMenuIconSpecifier.With(TurnOnTexture),
            };
        }

        options.Add(option);
        return options;
    }

    private void HandleSendToggle()
    {
        var msg = new EmitterToggleActiveMessage();
        SendPredictedMessage(msg);
    }
}
