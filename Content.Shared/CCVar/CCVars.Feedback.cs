using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Used to set what popups are shown. Can accept multiple origins, just use spaces!
    /// </summary>
    /// <example>
    /// wizden deltav
    /// </example>
    public static readonly CVarDef<string> FeedbackValidOrigins =
        CVarDef.Create("feedback.valid_origins", "", CVar.SERVER);
}
