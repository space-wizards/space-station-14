using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Used to set what popups are shown. Can accept multiple origins, just use spaces! See
    /// <see cref="Content.Shared.FeedbackSystem.FeedbackPopupPrototype">FeedbackPopupPrototype</see>'s <see cref="Content.Shared.FeedbackSystem.FeedbackPopupPrototype.PopupOrigin">PopupOrigin</see> field.
    /// Only prototypes who's PopupOrigin matches one of the FeedbackValidOrigins will be shown to players.
    /// </summary>
    /// <example>
    /// wizden deltav
    /// </example>
    public static readonly CVarDef<string> FeedbackValidOrigins =
        CVarDef.Create("feedback.valid_origins", "", CVar.SERVER | CVar.REPLICATED);
}
