using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    public static readonly CVarDef<float> MhelpRateLimitPeriod =
        CVarDef.Create("mhelp.rate_limit_period", 2f, CVar.SERVERONLY);

    public static readonly CVarDef<int> MhelpRateLimitCount =
        CVarDef.Create("mhelp.rate_limit_count", 10, CVar.SERVERONLY);

    public static readonly CVarDef<string> MHelpWebhook =
        CVarDef.Create("discord.mhelp_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> MHelpSound =
        CVarDef.Create("audio.mhelp_sound", "/Audio/_Starlight/Effects/hello_mentor.ogg", CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> MHelpPing =
        CVarDef.Create("mhelp.ping_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY | CVar.ARCHIVE);
}
