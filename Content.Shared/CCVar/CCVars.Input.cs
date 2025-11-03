using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///  Soma actual comment
    /// </summary>
    public static readonly CVarDef<bool> TakeBeforeStorage =
        CVarDef.Create("input.take_before_storage", true, CVar.CLIENT | CVar.REPLICATED);
}
