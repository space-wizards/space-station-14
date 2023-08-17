using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Prototypes;

/// <summary>
/// This is a prototype for a cargo bounty, a set of items
/// that must be sold together in a labeled container in order
/// to receive a monetary reward.
/// </summary>
[Prototype("cargoBounty"), Serializable, NetSerializable]
public sealed class CargoBountyPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The monetary reward for completing the bounty
    /// </summary>
    [DataField("reward", required: true)]
    public int Reward { get; private set; }

    /// <summary>
    /// A description for flava purposes.
    /// </summary>
    [DataField("description")]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The entries that must be satisfied for the cargo bounty to be complete.
    /// </summary>
    [DataField("entries", required: true)]
    public List<CargoBountyItemEntry> Entries { get; private set; } = new();
}

[DataDefinition, Serializable, NetSerializable]
public readonly partial record struct CargoBountyItemEntry()
{
    /// <summary>
    /// A whitelist for determining what items satisfy the entry.
    /// </summary>
    [DataField("whitelist", required: true)]
    public EntityWhitelist Whitelist { get; private init; }  = default!;

    // todo: implement some kind of simple generic condition system

    /// <summary>
    /// How much of the item must be present to satisfy the entry
    /// </summary>
    [DataField("amount")]
    public int Amount { get; private init; }  = 1;

    /// <summary>
    /// A player-facing name for the item.
    /// </summary>
    [DataField("name")]
    public string Name { get; private init; }  = string.Empty;
}
