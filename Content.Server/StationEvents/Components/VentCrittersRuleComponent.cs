// Modifications ported by Ronstation from Delta-V, therefore this file is licensed as MIT sublicensed with AGPL-v3.0.
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
    [DataField("min")]
    public int Min = 3; // Ronstation - Changed from 2 to 3

    /// <summary>
    /// DeltaV: Base maximum number of critters to spawn.
    /// </summary>
    [DataField("max")]
    public int Max = 4; // Ronstation - Changed from 3 to 4

    /// <summary>
    /// DeltaV: Min and max get multiplied by the player count then divided by this.
    /// </summary>
    [DataField("playerRatio")]
    public int PlayerRatio = 20; // Ronstation - Lowered PR from 25 to 20 to scale higher

    /// <summary>
    /// Ronstation: Cap for how many critters can be spawned, used in the calculation for count.
    /// </summary>
    [DataField("ceiling")]
    public int Ceiling = 8;

    /// <summary>
    /// Ronstation: Floor for how many critters can be spawned, used in the calculation for count.
    /// </summary>
    [DataField("floor")]
    public int Floor = 3;
}
