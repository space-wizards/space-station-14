using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using System.Collections.Generic;
using Content.Shared.Tag;

namespace Content.Shared.Composting;

[RegisterComponent]
public sealed partial class CompostingComponent : Component
{
    /// <summary>
    /// List of tags allowed for composting (e.g., "Fruit", "Egg", "Meat").
    /// </summary>
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdListSerializer<TagPrototype>))]
    public List<string> Whitelist = new();

    /// <summary>
    /// Time in minutes for each item to be composted.
    /// </summary>
    [DataField("compostTime")]
    public float CompostTime = 20.0f;

    /// <summary>
    /// Items currently being composted and their completion times.
    /// </summary>
    [DataField("compostingItems")]
    public Dictionary<EntityUid, TimeSpan> CompostingItems = new();

    /// <summary>
    /// Amount of finished compost ready to be collected.
    /// </summary>
    [DataField("readyCompost")]
    public int ReadyCompost = 0;

    /// <summary>
    /// Maximum items amount (composting + ready compost) that it can hold
    /// </summary>
    [DataField("maxCapacity")]
    public int MaxCapacity = 10;
}