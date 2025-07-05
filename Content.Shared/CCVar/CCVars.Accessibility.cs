using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Chat window opacity slider, controlling the alpha of the chat window background.
    ///     Goes from to 0 (completely transparent) to 1 (completely opaque)
    /// </summary>
    public static readonly CVarDef<float> ChatWindowOpacity =
        CVarDef.Create("accessibility.chat_window_transparency", 0.85f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Toggle for visual effects that may potentially cause motion sickness.
    ///     Where reasonable, effects affected by this CVar should use an alternate effect.
    ///     Please do not use this CVar as a bandaid for effects that could otherwise be made accessible without issue.
    /// </summary>
    public static readonly CVarDef<bool> ReducedMotion =
        CVarDef.Create("accessibility.reduced_motion", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ChatEnableColorName =
        CVarDef.Create("accessibility.enable_color_name",
            true,
            CVar.CLIENTONLY | CVar.ARCHIVE,
            "Toggles displaying names with individual colors.");

    /// <summary>
    ///     Screen shake intensity slider, controlling the intensity of the CameraRecoilSystem.
    ///     Goes from 0 (no recoil at all) to 1 (regular amounts of recoil)
    /// </summary>
    public static readonly CVarDef<float> ScreenShakeIntensity =
        CVarDef.Create("accessibility.screen_shake_intensity", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     A generic toggle for various visual effects that are color sensitive.
    ///     As of 2/16/24, only applies to progress bar colors.
    /// </summary>
    public static readonly CVarDef<bool> AccessibilityColorblindFriendly =
        CVarDef.Create("accessibility.colorblind_friendly", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Speech bubble text opacity slider, controlling the alpha of speech bubble's text.
    ///     Goes from to 0 (completely transparent) to 1 (completely opaque)
    /// </summary>
    public static readonly CVarDef<float> SpeechBubbleTextOpacity =
        CVarDef.Create("accessibility.speech_bubble_text_opacity", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Speech bubble speaker opacity slider, controlling the alpha of the speaker's name in a speech bubble.
    ///     Goes from to 0 (completely transparent) to 1 (completely opaque)
    /// </summary>
    public static readonly CVarDef<float> SpeechBubbleSpeakerOpacity =
        CVarDef.Create("accessibility.speech_bubble_speaker_opacity", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Speech bubble background opacity slider, controlling the alpha of the speech bubble's background.
    ///     Goes from to 0 (completely transparent) to 1 (completely opaque)
    /// </summary>
    public static readonly CVarDef<float> SpeechBubbleBackgroundOpacity =
        CVarDef.Create("accessibility.speech_bubble_background_opacity", 0.75f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If enabled, censors character nudity by forcing clothes markings on characters, selected by the client.
    /// Both this and AccessibilityServerCensorNudity must be false to display nudity on the client.
    /// </summary>
    public static readonly CVarDef<bool> AccessibilityClientCensorNudity =
        CVarDef.Create("accessibility.censor_nudity", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If enabled, censors character nudity by forcing clothes markings on characters, selected by the server.
    /// Both this and AccessibilityClientCensorNudity must be false to display nudity on the client.
    /// </summary>
    public static readonly CVarDef<bool> AccessibilityServerCensorNudity =
            CVarDef.Create("accessibility.server_censor_nudity", false, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
}
