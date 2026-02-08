using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Max entities each player can spawn within one <see cref="SandboxEntitySpawnTimeFrameLengthSeconds"/>
    /// during sandbox mode.
    /// </summary>
    public static readonly CVarDef<int>
        SandboxMaxEntitySpawnsPerTimeFrame = CVarDef.Create("sandbox.max_entity_spawns_per_time_frame",
            20,
            CVar.ARCHIVE | CVar.SERVERONLY);

    /// <summary>
    /// Length of the time frame in which to count the number of a player's recent entity spawns
    /// and compare against the allowed maximum.
    /// </summary>
    public static readonly CVarDef<float>
        SandboxEntitySpawnTimeFrameLengthSeconds = CVarDef.Create("sandbox.entity_spawn_time_frame_length_seconds",
            5.0f,
            CVar.ARCHIVE | CVar.SERVERONLY);
}
