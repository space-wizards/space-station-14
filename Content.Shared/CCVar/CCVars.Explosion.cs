using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     How many tiles the explosion system will process per tick
    /// </summary>
    /// <remarks>
    ///     Setting this too high will put a large load on a single tick. Setting this too low will lead to
    ///     unnaturally "slow" explosions.
    /// </remarks>
    public static readonly CVarDef<int> ExplosionTilesPerTick =
        CVarDef.Create("explosion.tiles_per_tick", 100, CVar.SERVERONLY);

    /// <summary>
    ///     Upper limit on the size of an explosion before physics-throwing is disabled.
    /// </summary>
    /// <remarks>
    ///     Large nukes tend to generate a lot of shrapnel that flies through space. This can functionally cripple
    ///     the server TPS for a while after an explosion (or even during, if the explosion is processed
    ///     incrementally.
    /// </remarks>
    public static readonly CVarDef<int> ExplosionThrowLimit =
        CVarDef.Create("explosion.throw_limit", 400, CVar.SERVERONLY);

    /// <summary>
    ///     If this is true, explosion processing will pause the NodeGroupSystem to pause updating.
    /// </summary>
    /// <remarks>
    ///     This only takes effect if an explosion needs more than one tick to process (i.e., covers more than <see
    ///     cref="ExplosionTilesPerTick"/> tiles). If this is not enabled, the node-system will rebuild its graph
    ///     every tick as the explosion shreds the station, causing significant slowdown.
    /// </remarks>
    public static readonly CVarDef<bool> ExplosionSleepNodeSys =
        CVarDef.Create("explosion.node_sleep", true, CVar.SERVERONLY);

    /// <summary>
    ///     Upper limit on the total area that an explosion can affect before the neighbor-finding algorithm just
    ///     stops. Defaults to a 60-rile radius explosion.
    /// </summary>
    /// <remarks>
    ///     Actual area may be larger, as it currently doesn't terminate mid neighbor finding. I.e., area may be that of a ~51 tile radius circle instead.
    /// </remarks>
    public static readonly CVarDef<int> ExplosionMaxArea =
        CVarDef.Create("explosion.max_area", (int)3.14f * 256 * 256, CVar.SERVERONLY);

    /// <summary>
    ///     Upper limit on the number of neighbor finding steps for the explosion system neighbor-finding algorithm.
    /// </summary>
    /// <remarks>
    ///     Effectively places an upper limit on the range that any explosion can have. In the vast majority of
    ///     instances, <see cref="ExplosionMaxArea"/> will likely be hit before this becomes a limiting factor.
    /// </remarks>
    public static readonly CVarDef<int> ExplosionMaxIterations =
        CVarDef.Create("explosion.max_iterations", 500, CVar.SERVERONLY);

    /// <summary>
    ///     Max Time in milliseconds to spend processing explosions every tick.
    /// </summary>
    /// <remarks>
    ///     This time limiting is not perfectly implemented. Firstly, a significant chunk of processing time happens
    ///     due to queued entity deletions, which happen outside of the system update code. Secondly, explosion
    ///     spawning cannot currently be interrupted & resumed, and may lead to exceeding this time limit.
    /// </remarks>
    public static readonly CVarDef<float> ExplosionMaxProcessingTime =
        CVarDef.Create("explosion.max_tick_time", 7f, CVar.SERVERONLY);

    /// <summary>
    ///     If the explosion is being processed incrementally over several ticks, this variable determines whether
    ///     updating the grid tiles should be done incrementally at the end of every tick, or only once the explosion has finished processing.
    /// </summary>
    /// <remarks>
    ///     The most notable consequence of this change is that explosions will only punch a hole in the station &
    ///     create a vacumm once they have finished exploding. So airlocks will no longer slam shut as the explosion
    ///     expands, just suddenly at the end.
    /// </remarks>
    public static readonly CVarDef<bool> ExplosionIncrementalTileBreaking =
        CVarDef.Create("explosion.incremental_tile", false, CVar.SERVERONLY);

    /// <summary>
    ///     This determines for how many seconds an explosion should stay visible once it has finished expanding.
    /// </summary>
    public static readonly CVarDef<float> ExplosionPersistence =
        CVarDef.Create("explosion.persistence", 1.0f, CVar.SERVERONLY);

    /// <summary>
    ///     If an explosion covers a larger area than this number, the damaging/processing will always start during
    ///     the next tick, instead of during the same tick that the explosion was generated in.
    /// </summary>
    /// <remarks>
    ///     This value can be used to ensure that for large explosions the area/tile calculation and the explosion
    ///     processing/damaging occurs in separate ticks. This helps reduce the single-tick lag if both <see
    ///     cref="ExplosionMaxProcessingTime"/> and <see cref="ExplosionTilesPerTick"/> are large. I.e., instead of
    ///     a single tick explosion, this cvar allows for a configuration that results in a two-tick explosion,
    ///     though most of the computational cost is still in the second tick.
    /// </remarks>
    public static readonly CVarDef<int> ExplosionSingleTickAreaLimit =
        CVarDef.Create("explosion.single_tick_area_limit", 400, CVar.SERVERONLY);

    /// <summary>
    ///     Whether or not explosions are allowed to create tiles that have
    ///     <see cref="ContentTileDefinition.MapAtmosphere"/> set to true.
    /// </summary>
    public static readonly CVarDef<bool> ExplosionCanCreateVacuum =
        CVarDef.Create("explosion.can_create_vacuum", true, CVar.SERVERONLY);
}
