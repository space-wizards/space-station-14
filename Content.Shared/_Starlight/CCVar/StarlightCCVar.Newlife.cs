using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    /// <summary>
    /// THe ammount of new lifes a player can have in a round.
    /// </summary>
    public static readonly CVarDef<int> MaxNewLifes =
        CVarDef.Create("newlife.max_new_lifes", 5, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE,
            "The maximum number of new lifes a player can have in a round.");
}
