using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.Backmen.CCVar;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class CCVars
{
    /*
     * Blob
     */

    public static readonly CVarDef<int> BlobMax =
        CVarDef.Create("blob.max", 3, CVar.SERVERONLY);

    public static readonly CVarDef<int> BlobPlayersPer =
        CVarDef.Create("blob.players_per", 20, CVar.SERVERONLY);


    public static readonly CVarDef<bool>
        EconomyWagesEnabled = CVarDef.Create("economy.wages_enabled", true, CVar.SERVERONLY);
}
