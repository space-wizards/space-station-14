using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> ConsoleLoginLocal =
        CVarDef.Create("console.loginlocal", true, CVar.ARCHIVE | CVar.SERVERONLY);

    /// <summary>
    ///     Automatically log in the given user as host, equivalent to the <c>promotehost</c> command.
    /// </summary>
    public static readonly CVarDef<string> ConsoleLoginHostUser =
        CVarDef.Create("console.login_host_user", "", CVar.ARCHIVE | CVar.SERVERONLY);
}
