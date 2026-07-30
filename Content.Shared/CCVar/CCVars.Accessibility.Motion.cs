using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    // CVars in this file should exist to make visual effects less harmful for players with motion sensitivity or photosensitivity.
    // These CVars are deliberately modular to allow users more agency over their experience in game.
    // Please do not use CVars as a bandaid for effects that could otherwise be made accessible without issue.

    /// <summary>
    ///     General entity-specific reduced motion setting.
    ///     Where possible, you should add new Cvars instead of using this.
    ///     This Cvar exists to interact with OptionsVisualizerSystem
    ///     to allow entities to define custom sprite layers with reduced motion.
    /// </summary>
    public static readonly CVarDef<bool> ReducedMotion =
        CVarDef.Create("accessibility.reduced_motion", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Replaces the AI static camera effect with a plain gradient.
    /// </summary>
    public static readonly CVarDef<bool> DisableAiStatic =
        CVarDef.Create("accessibility.disable_ai_static", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Replaces the movement in the blurry shader for a static effect.
    /// </summary>
    public static readonly CVarDef<bool> DisableBlurryVision =
        CVarDef.Create("accessibility.disable_blurry_vision", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Replaces the movement in the drunk shader for a static offset.
    /// </summary>
    public static readonly CVarDef<bool> DisableDrunkOverlay =
        CVarDef.Create("accessibility.disable_drunk_overlay", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Replaces 'flash' effect with a reduced movement equivalent.
    /// </summary>
    public static readonly CVarDef<bool> DisableFlashEffect =
        CVarDef.Create("accessibility.disable_flash_effect", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Decreases motion on the heat distortion shader.
    /// </summary>
    public static readonly CVarDef<bool> DisableHeatDistortion =
        CVarDef.Create("accessibility.disable_heat_distortion", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Disables the noise effect on the night vision overlay.
    /// </summary>
    public static readonly CVarDef<bool> DisableNightVisionNoise =
        CVarDef.Create("accessibility.disable_nv_noise", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Completely disables the rainbow overlay used for the 'high' effect.
    /// </summary>
    public static readonly CVarDef<bool> DisableRainbowOverlay =
        CVarDef.Create("accessibility.disable_rainbow_overlay", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Disables the screen warping effect on the singularity.
    /// </summary>
    public static readonly CVarDef<bool> DisableSinguloWarp =
        CVarDef.Create("accessibility.disable_singulo_warp", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
