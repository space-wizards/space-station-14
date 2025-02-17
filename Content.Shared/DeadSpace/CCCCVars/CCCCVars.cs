using Robust.Shared.Configuration;

namespace Content.Shared.DeadSpace.CCCCVars;

/// <summary>
///     DeadSpace modules console variables
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming
public sealed class CCCCVars
{
    /*
	* GCF
	*/

    /// <summary>
    ///     Whether GCF being shown is enabled at all.
    /// </summary>

    public static readonly CVarDef<bool> GCFEnabled =
        CVarDef.Create("gcf_auto.enabled", true);

    /// <summary>
    ///     Notify for admin about GCF Clean.
    /// </summary>
    public static readonly CVarDef<bool> GCFNotify =
        CVarDef.Create("gcf_auto.notify", false);

    /// <summary>
    ///     The number of seconds between each GCF
    /// </summary>
    public static readonly CVarDef<float> GCFFrequency =
        CVarDef.Create("gcf_auto.frequency", 300f);

    /*
	* InfoLinks
	*/

    /// <summary>
    /// IPs address for reconnect.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksIPs =
        CVarDef.Create("infolinks.ips", string.Empty, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Multiplier for playtime.
    /// </summary>
    public static readonly CVarDef<float> PlayTimeMultiplier =
        CVarDef.Create("playtime.multiplier", 1f, CVar.SERVER | CVar.REPLICATED);

    /*
	* TTS
	*/

    public static readonly CVarDef<float> TTSVolumeRadio =
        CVarDef.Create("tts.volume_radio", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RadioTTSSoundsEnabled =
        CVarDef.Create("audio.radio_tts_sounds_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
    * Typan
    */

    /// <summary>
    /// Should Typan spawn or not.
    /// </summary>
    public static readonly CVarDef<bool> TypanEnabled =
        CVarDef.Create("typan.enabled", false, CVar.SERVERONLY);

    /*
    * Lobby ui
    */

    /// <summary>
    /// Lobby default background. Can be Parallax or Image.
    /// </summary>
    public static readonly CVarDef<string> Background =
        CVarDef.Create("ui.background", "Image", CVar.CLIENTONLY | CVar.ARCHIVE);
}
