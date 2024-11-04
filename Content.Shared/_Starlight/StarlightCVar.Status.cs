using Robust.Shared.Configuration;

namespace Content.Shared.Starlight;
public sealed partial class StarlightCVar
{
    /// <summary>
    /// Status webhook
    /// </summary>
    public static readonly CVarDef<string> StatusWebhook =
        CVarDef.Create("discord.status_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> DiscordKey =
        CVarDef.Create("discord.api_key", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

}
