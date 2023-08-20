using Content.Shared.Access;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Random;

/// <summary>
/// Rules-based item selection. Can be used for any sort of conditional selection
/// Every single condition needs to be true for this to be selected.
/// e.g. "choose maintenance audio if 90% of tiles nearby are maintenance tiles"
/// </summary>
[Prototype("rules")]
public sealed class RulesPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField("rules", required: true)]
    public List<RulesRule> Rules = new();
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class RulesRule
{

}

/// <summary>
/// Returns true if the attached entity is in space.
/// </summary>
public sealed partial class InSpaceRule : RulesRule
{

}

/// <summary>
/// Checks for entities matching the whitelist in range.
/// This is more expensive than <see cref="NearbyComponentsRule"/> so prefer that!
/// </summary>
public sealed partial class NearbyEntitiesRule : RulesRule
{
    /// <summary>
    /// How many of the entity need to be nearby.
    /// </summary>
    [DataField("count")]
    public int Count = 1;

    [DataField("whitelist", required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField("range")]
    public float Range = 10f;
}

public sealed partial class NearbyTilesPercentRule : RulesRule
{
    /// <summary>
    /// If there are anchored entities on the tile do we ignore the tile.
    /// </summary>
    [DataField("ignoreAnchored")] public bool IgnoreAnchored;

    [DataField("percent", required: true)]
    public float Percent;

    [DataField("tiles", required: true, customTypeSerializer:typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
    public List<string> Tiles = new();

    [DataField("range")]
    public float Range = 10f;
}

/// <summary>
/// Always returns true. Used for fallbacks.
/// </summary>
public sealed partial class AlwaysTrueRule : RulesRule
{

}

/// <summary>
/// Returns true if on a grid or in range of one.
/// </summary>
public sealed partial class GridInRangeRule : RulesRule
{
    [DataField("range")]
    public float Range = 10f;

    [DataField("inverted")]
    public bool Inverted = false;
}

/// <summary>
/// Returns true if griduid and mapuid match (AKA on 'planet').
/// </summary>
public sealed partial class OnMapGridRule : RulesRule
{

}

/// <summary>
/// Checks for an entity nearby with the specified access.
/// </summary>
public sealed partial class NearbyAccessRule : RulesRule
{
    // This exists because of doorelectronics contained inside doors.
    /// <summary>
    /// Does the access entity need to be anchored.
    /// </summary>
    [DataField("anchored")]
    public bool Anchored = true;

    /// <summary>
    /// Count of entities that need to be nearby.
    /// </summary>
    [DataField("count")]
    public int Count = 1;

    [DataField("access", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<AccessLevelPrototype>))]
    public List<string> Access = new();

    [DataField("range")]
    public float Range = 10f;
}

public sealed partial class NearbyComponentsRule : RulesRule
{
    /// <summary>
    /// Does the entity need to be anchored.
    /// </summary>
    [DataField("anchored")]
    public bool Anchored;

    [DataField("count")] public int Count;

    [DataField("components", required: true)]
    public ComponentRegistry Components = default!;

    [DataField("range")]
    public float Range = 10f;
}
