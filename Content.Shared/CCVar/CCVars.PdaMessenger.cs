using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether PDA messaging is enabled.
    /// </summary>
    public static readonly CVarDef<bool> PdaMessengerEnabled =
        CVarDef.Create("pda_messenger.enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     The maximum char length of a PDA message.
    /// </summary>
    public static readonly CVarDef<int> PdaMessengerMaxMessageLength =
        CVarDef.Create("pda_messenger.max_message_length", 100, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     How many messages per conversation the server and each PDA keep.
    /// </summary>
    public static readonly CVarDef<int> PdaMessengerHistoryCap =
        CVarDef.Create("pda_messenger.history_cap", 100, CVar.SERVERONLY);
}
