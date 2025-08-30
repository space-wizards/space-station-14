using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Time interval dictating how often parrots that draw from the database refresh their memory cache
    /// </summary>
    public static readonly CVarDef<TimeSpan> ParrotDbRefreshInterval =
        CVarDef.Create("parrot.db_refresh_interval", TimeSpan.FromMinutes(10), CVar.SERVER);

    /// <summary>
    /// The number of memories parrots retrieve from the database upon refreshing the memory
    /// </summary>
    public static readonly CVarDef<int> ParrotDbRefreshNumMemories =
        CVarDef.Create("parrot.db_refresh_num_memories", 15, CVar.SERVER);

    /// <summary>
    /// Minimum overall playtime a player needs for their messages to be committed to the database by a parrot
    /// </summary>
    public static readonly CVarDef<TimeSpan> ParrotMinimumPlaytimeFilter =
        CVarDef.Create("parrot.db_min_overall_playtime", TimeSpan.FromHours(5), CVar.SERVER);

    /// <summary>
    /// Maximum age of parrot memories stored in the database.  Memories are cleaned up every round
    /// </summary>
    public static readonly CVarDef<TimeSpan> ParrotMaximumMemoryAge =
        CVarDef.Create("parrot.db_max_memory_age", TimeSpan.FromDays(7), CVar.SERVER);
}
