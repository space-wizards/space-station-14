using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<TimeSpan> ParrotDbRefreshInterval = CVarDef.Create(
        "parrot.db_refresh_interval",
        TimeSpan.FromMinutes(10),
        CVar.SERVER,
        "Time interval dictating how often parrots that draw from the database refresh their message cache."
    );

    public static readonly CVarDef<TimeSpan> ParrotMinimumPlaytimeFilter = CVarDef.Create(
        "parrot.db_min_overall_playtime",
        TimeSpan.FromHours(5),
        CVar.SERVER,
        "Minimum overall playtime a player needs for their messages to be committed to the database by a parrot"
    );

    public static readonly CVarDef<TimeSpan> ParrotMaximumMessageAge = CVarDef.Create(
        "parrot.db_max_message_age",
        TimeSpan.FromDays(30),
        CVar.SERVER,
        "Maximum age of parrot messages stored in the database.  Messages are cleaned up every round."
    );
}
