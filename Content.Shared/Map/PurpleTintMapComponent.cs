using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Map;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PurpleTintMapComponent : Component
{
    // Whether tint overlay should be active on this map.
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    // Optional overrides. If null, client uses overlay defaults.
    [DataField, AutoNetworkedField]
    public Color? TintColor;

    [DataField, AutoNetworkedField]
    public float? Strength;

    [DataField, AutoNetworkedField]
    public float? Saturation;

    [DataField, AutoNetworkedField]
    public float? Contrast;
}
