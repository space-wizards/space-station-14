using Content.Server.StationEvents.Events;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Storage;
using Robust.Shared.Map; // DeltaV

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(VentCrittersRule))]
public sealed partial class VentCrittersRuleComponent : Component
{
    // DeltaV: Replaced by Table
    //[DataField("entries")]
    //public List<EntitySpawnEntry> Entries = new();

    /// <summary>
    /// DeltaV: Table of possible entities to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// At least one special entry is guaranteed to spawn
    /// </summary>
    [DataField("specialEntries")]
    public List<EntitySpawnEntry> SpecialEntries = new();

    /// <summary>
    /// DeltaV: The location of the vent that got picked.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates? Location;

    /// <summary>
    /// DeltaV: Base minimum number of critters to spawn.
    /// </summary>
    [DataField]
    public int Min = 2;

    /// <summary>
    /// DeltaV: Base maximum number of critters to spawn.
    /// </summary>
    [DataField]
    public int Max = 3;

    /// <summary>
    /// DeltaV: Min and max get multiplied by the player count then divided by this.
    /// </summary>
    [DataField]
    public int PlayerRatio = 25;
}
