using Content.Shared.Maps;
using Robust.Shared.Prototypes;

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

public sealed class NearbyTilesRule : RulesRule
{
    [DataField("percentage", required: true)]
    public float Percentage;

    [DataField("tile", required: true, customTypeSerializer:typeof(ContentTileDefinition))]
    public string Tile = string.Empty;
}

/// <summary>
/// Always returns true. Used for fallbacks.
/// </summary>
public sealed class TrueRule : RulesRule
{

}
