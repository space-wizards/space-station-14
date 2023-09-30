using Content.Server.Objectives.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that you steal a certain item.
/// </summary>
[RegisterComponent, Access(typeof(StealConditionSystem))]
public sealed partial class StealConditionComponent : Component
{
    /// <summary>
    /// The id of the item to steal.
    /// </summary>
    /// <remarks>
    /// Works by prototype id not tags or anything so it has to be the exact item.
    /// </remarks>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Prototype = string.Empty;

    /// <summary>
    /// Help newer players by saying e.g. "steal the chief engineer's advanced magboots"
    /// instead of "steal advanced magboots. Should be a loc string.
    /// </summary>
    [DataField("owner"), ViewVariables(VVAccess.ReadWrite)]
    public string? OwnerText;
}
