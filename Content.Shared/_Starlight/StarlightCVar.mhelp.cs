using Robust.Shared.Configuration;

namespace Content.Shared.Starlight;
public sealed partial class StarlightCVar
{
    public static readonly CVarDef<float> MhelpRateLimitPeriod =
        CVarDef.Create("mhelp.rate_limit_period", 2f, CVar.SERVERONLY);

    public static readonly CVarDef<int> MhelpRateLimitCount =
        CVarDef.Create("mhelp.rate_limit_count", 10, CVar.SERVERONLY);

    public static readonly CVarDef<string> MHelpWebhook =
        CVarDef.Create("discord.mhelp_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
