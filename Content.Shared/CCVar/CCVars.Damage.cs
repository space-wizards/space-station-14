using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Percent chance that a prone mob will be hit by projectiles
    ///     rather than passing through harmlessly.
    /// </summary>
    public static readonly CVarDef<int> ProneMobHitChance =
        CVarDef.Create("damage.prone_mob_hit_chance", 0, CVar.REPLICATED);
}