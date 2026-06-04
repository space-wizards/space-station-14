using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Maximum number of nearby linked portals to preload per player.
    ///     Each selected portal subscribes the player to both the portal and its linked portal views.
    /// </summary>
    public static readonly CVarDef<int> PortalMaxPreloaded =
        CVarDef.Create("portal.max_preloaded", 2, CVar.SERVERONLY);
}
