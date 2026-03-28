//HONK - Hover tooltip CVars (partial class in upstream dir, needs same namespace)
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether to show entity name tooltip on hover.
    /// </summary>
    public static readonly CVarDef<bool> HoverTooltipEnabled =
        CVarDef.Create("hud.hover_tooltip_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Delay in seconds before the hover tooltip appears.
    /// </summary>
    public static readonly CVarDef<float> HoverTooltipDelay =
        CVarDef.Create("hud.hover_tooltip_delay", 0.3f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
