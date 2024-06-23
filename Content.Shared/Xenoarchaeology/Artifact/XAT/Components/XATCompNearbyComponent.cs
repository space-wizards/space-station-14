using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used a XAT that activates when an entity fulfilling the given whitelist is nearby the artifact.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class XATCompNearbyComponent : Component
{
    [DataField(customTypeSerializer: typeof(ComponentNameSerializer))]
    public string Component;

    [DataField]
    public float Radius = 5;

    [DataField]
    public int Count = 1;
}
