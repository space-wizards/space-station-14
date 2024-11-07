using Robust.Shared.Configuration;

namespace Content.Shared.Starlight;
public sealed partial class StarlightCVar
{
    /// <summary>
    /// Status webhook
    /// </summary>
    public static readonly CVarDef<string> StatusWebhook =
        CVarDef.Create("discord.status_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> StatusStaffWebhook =
         CVarDef.Create("discord.status_staff_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<ulong> StatusMessageId =
        CVarDef.Create("discord.status_message_id", 0UL, CVar.SERVERONLY);

    public static readonly CVarDef<ulong> StatusMessageStaffId =
        CVarDef.Create("discord.status_staff_message_id", 0UL, CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordKey =
        CVarDef.Create("discord.api_key", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

}
