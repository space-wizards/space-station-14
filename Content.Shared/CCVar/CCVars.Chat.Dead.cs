using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// True: dead players can send dead chat
    /// False: dead players can't send dead chat
    /// </summary>
    public static readonly CVarDef<bool> DeadChatEnabled =
        CVarDef.Create("dead_chat.enabled", true, CVar.NOTIFY | CVar.REPLICATED);
}
