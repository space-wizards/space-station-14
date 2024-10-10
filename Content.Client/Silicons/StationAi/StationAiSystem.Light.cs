using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Utility;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    // Used for surveillance camera lights

    private void InitializePowerToggle()
    {
        SubscribeLocalEvent<ItemTogglePointLightComponent, GetStationAiRadialEvent>(OnLightGetRadial);
    }

    private void OnLightGetRadial(Entity<ItemTogglePointLightComponent> ent, ref GetStationAiRadialEvent args)
    {
        if (!TryComp(ent.Owner, out ItemToggleComponent? toggle))
            return;

        args.Actions.Add(new StationAiRadial()
        {
            Tooltip = Loc.GetString("toggle-light"),
            Sprite = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Event = new StationAiLightEvent()
            {
                Enabled = !toggle.Activated
            }
        });
    }
}
