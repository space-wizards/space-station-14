using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used a XAT that activates when an entity fulfilling the given whitelist is nearby the artifact.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATCompNearbyComponent)), AutoGenerateComponentState]
public sealed partial class XATCompNearbyComponent : Component
{
    /// <summary>
    /// Component name that is required to activate trigger.
    /// Is spelled without 'Component' suffix.
    /// </summary>
    [DataField(customTypeSerializer: typeof(ComponentNameSerializer)), AutoNetworkedField]
    public string RequireComponentWithName = "Item";

    [DataField, AutoNetworkedField]
    public float Radius = 5;

    [DataField, AutoNetworkedField]
    public int Count = 1;
}
