using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Impstation.CCVar;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class ImpCCVars : CVars
{
    /// <summary>
    /// Toggles the proximity warping effect on the singularity.
    /// This option is for people who generally do not mind motion, but find
    /// the singularity warping especially egregious.
    /// </summary>
    public static readonly CVarDef<bool> DisableSinguloWarping =
        CVarDef.Create("accessibility.disable_singulo_warping", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Should the player automatically get up after being knocked down
    /// </summary>
    public static readonly CVarDef<bool> AutoGetUp =
        CVarDef.Create("white.auto_get_up", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED); // WD EDIT

    /// <summary>
    /// The number of shared moods to give thaven by default.
    /// </summary>
    public static readonly CVarDef<uint> ThavenSharedMoodCount =
        CVarDef.Create<uint>("thaven.shared_mood_count", 1, CVar.SERVERONLY);

    /// <summary>
    /// A string containing a list of newline-separated words to be highlighted in the chat.
    /// </summary>
    public static readonly CVarDef<string> ChatHighlights =
        CVarDef.Create("chat.highlights", "", CVar.CLIENTONLY | CVar.ARCHIVE, "A list of newline-separated words to be highlighted in the chat.");

    /// <summary>
    /// An option to toggle the automatic filling of the highlights with the character's info, if available.
    /// </summary>
    public static readonly CVarDef<bool> ChatAutoFillHighlights =
        CVarDef.Create("chat.auto_fill_highlights", false, CVar.CLIENTONLY | CVar.ARCHIVE, "Toggles automatically filling the highlights with the character's information.");

    /// <summary>
    /// The color in which the highlights will be displayed.
    /// </summary>
    public static readonly CVarDef<string> ChatHighlightsColor =
        CVarDef.Create("chat.highlights_color", "#17FFC1FF", CVar.CLIENTONLY | CVar.ARCHIVE, "The color in which the highlights will be displayed.");

    public static readonly CVarDef<int> CosmicCultistEntropyValue =
        CVarDef.Create("cosmiccult.cultist_entropy_value", 7, CVar.SERVER, "How much entropy a convert is worth towards the next monument tier");

    public static readonly CVarDef<int> CosmicCultTargetConversionPercent =
        CVarDef.Create("cosmiccult.target_conversion_percent", 40, CVar.SERVER, "How much of the crew the cult is aiming to convert for a tier 3 monument");

    public static readonly CVarDef<int> CosmicCultExtraEntropyForFinale =
        CVarDef.Create<int>("cosmiccult.extra_entropy_for_finale", 20, CVar.SERVER, "How much additional entropy is required to initiate the finale from a tier 3 monument");
}
