using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<TimeSpan> ParrotDbRefreshInterval = CVarDef.Create(
        "parrot.db_refresh_interval",
        TimeSpan.FromMinutes(10),
        CVar.SERVER,
        "Time interval dictating how often parrots that draw from the database refresh their memory cache."
    );

    public static readonly CVarDef<TimeSpan> ParrotMinimumPlaytimeFilter = CVarDef.Create(
        "parrot.db_min_overall_playtime",
        TimeSpan.FromHours(5),
        CVar.SERVER,
        "Minimum overall playtime a player needs for their messages to be committed to the database by a parrot"
    );

    public static readonly CVarDef<TimeSpan> ParrotMaximumMemoryAge = CVarDef.Create(
        "parrot.db_max_memory_age",
        TimeSpan.FromDays(7),
        CVar.SERVER,
        "Maximum age of parrot memories stored in the database.  Memories are cleaned up every round."
    );
}
