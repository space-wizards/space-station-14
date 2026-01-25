using Content.Shared.Light.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    // Handles light toggling.

    private void InitializeLight()
    {
        SubscribeLocalEvent<ItemTogglePointLightComponent, StationAiLightEvent>(OnLight);
    }

    private void OnLight(EntityUid ent, ItemTogglePointLightComponent component, StationAiLightEvent args)
    {
        if (args.Enabled)
            _toggles.TryActivate(ent, user: args.User);
        else
            _toggles.TryDeactivate(ent, user: args.User);
    }
}

[Serializable, NetSerializable]
public sealed class StationAiLightEvent : BaseStationAiAction
{
    public bool Enabled;
}
