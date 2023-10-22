using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.NPC.Queries;

/// <summary>
/// Utility queries that run regularly to update an NPC without re-doing their thinking logic.
/// </summary>
[DataDefinition]
public sealed partial class UtilityService
{
    /// <summary>
    /// Identifier to use for this service. This is used to track its cooldown.
    /// </summary>
    [DataField("id", required: true)]
    public string ID = string.Empty;

    /// <summary>
    /// Prototype of the utility query.
    /// </summary>
    [DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<UtilityQueryPrototype>))]
    public string Prototype = string.Empty;

    [DataField("minCooldown")]
    public float MinCooldown = 0.25f;

    [DataField("maxCooldown")]
    public float MaxCooldown = 0.60f;

    /// <summary>
    /// Key to update with the utility query.
    /// </summary>
    [DataField("key", required: true)]
    public string Key = string.Empty;
}
