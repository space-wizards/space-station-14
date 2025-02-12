using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Atmos.Rotting;

/// <summary>
/// Lets an entity rot into another entity.
/// Used by raw meat to turn into rotten meat.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RotIntoComponent : Component
{
    /// <summary>
    /// Entity to rot into.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)), ViewVariables(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Entity = string.Empty;

    /// <summary>
    /// Rotting stage to turn at, this is a multiplier of the total rot time.
    /// 0 = rotting, 1 = bloated, 2 = extremely bloated
    /// </summary>
    [DataField, ViewVariables]
    public int Stage;
}
