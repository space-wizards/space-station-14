using Robust.Shared.Configuration;

namespace Content.Shared.Starlight;
public sealed partial class StarlightCVar
{
    /// <summary>
    /// Discord oAuth
    /// </summary>

    public static readonly CVarDef<string> DiscordCallback =
        CVarDef.Create("discord.callback", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<ulong> StatusMessageId =
        CVarDef.Create("discord.status_message_id", 0UL, CVar.SERVERONLY);

    public static readonly CVarDef<string> Secret =
        CVarDef.Create("discord.secret", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
