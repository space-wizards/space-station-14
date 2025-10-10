using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Map;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DesertMirageMapComponent : Component
{
    // Whether mirage overlay should be active on this map.
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    // Optional overrides. If null, client uses overlay defaults.
    [DataField, AutoNetworkedField]
    public float? Strength;

    [DataField, AutoNetworkedField]
    public float? Speed;

    [DataField, AutoNetworkedField]
    public float? DistortScale;

    [DataField, AutoNetworkedField]
    public float? VerticalBias;
}
