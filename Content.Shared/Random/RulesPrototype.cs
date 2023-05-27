using Content.Shared.Maps;
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

/*
 * TODO: Make an audio datadef that has an attached rules proto
 * Make a priority list of audio rules
 * Need a debug thing to check rules nearby
 * Play the full audio clip then re-check rules
 * Shuffle audio only once every rule played (then need to make sure next track isn't repeated)
 */

[ImplicitDataDefinitionForInheritors]
public abstract class RulesRule
{

}

/// <summary>
/// Returns true if the attached entity is in space.
/// </summary>
public sealed class InSpaceRule : RulesRule
{

}

public sealed class NearbyTilesPercentRule : RulesRule
{
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
public sealed class AlwaysTrueRule : RulesRule
{

}

/// <summary>
/// Returns true if griduid and mapuid match (AKA on 'planet').
/// </summary>
public sealed class OnMapGridRule : RulesRule
{

}

public sealed class NearbyComponentsRule : RulesRule
{
    [DataField("components", required: true)]
    public ComponentRegistry Components = default!;

    [DataField("range")]
    public float Range = 10f;
}
