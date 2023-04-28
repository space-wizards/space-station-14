using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.NPC.Queries;

/// <summary>
/// Utility queries that run regularly to update an NPC without re-doing their thinking logic.
/// </summary>
[DataDefinition]
public sealed class UtilityService
{
    /// <summary>
    /// Prototype of the utility query.
    /// </summary>
    [DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<UtilityQueryPrototype>))]
    public string Prototype = string.Empty;

    [DataField("minCooldown")]
    public float MinCooldown = 0.15f;

    [DataField("maxCooldown")]
    public float MaxCooldown = 0.25f;

    /// <summary>
    /// Key to update with the utility query.
    /// </summary>
    [DataField("key", required: true)]
    public string Key = string.Empty;
}
