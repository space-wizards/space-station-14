using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.Backmen.CCVar;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class CCVars
{
    public static readonly CVarDef<bool>
        EconomyWagesEnabled = CVarDef.Create("economy.wages_enabled", true, CVar.SERVERONLY);
}
