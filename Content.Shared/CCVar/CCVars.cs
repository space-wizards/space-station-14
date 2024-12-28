using Content.Shared._EinsteinEngines.Supermatter.Components;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

/// <summary>
/// Contains all the CVars used by content.
/// </summary>
/// <remarks>
/// NOTICE FOR FORKS: Put your own CVars in a separate file with a different [CVarDefs] attribute. RT will automatically pick up on it.
/// </remarks>
[CVarDefs]
public sealed partial class CCVars : CVars
{
    // Only debug stuff lives here.

    /// <summary>
    /// A simple toggle to test <c>OptionsVisualizerComponent</c>.
    /// </summary>
    public static readonly CVarDef<bool> DebugOptionVisualizerTest =
        CVarDef.Create("debug.option_visualizer_test", false, CVar.CLIENTONLY);

    /// DELTA-V CCVARS
    /*
     * Glimmer
     */

    /// <summary>
    ///    Whether glimmer is enabled.
    /// </summary>
    public static readonly CVarDef<bool> GlimmerEnabled =
        CVarDef.Create("glimmer.enabled", true, CVar.REPLICATED);

    /// <summary>
    ///     Passive glimmer drain per second.
    ///     Note that this is randomized and this is an average value.
    /// </summary>
    public static readonly CVarDef<float> GlimmerLostPerSecond =
        CVarDef.Create("glimmer.passive_drain_per_second", 0.1f, CVar.SERVERONLY);

    /// <summary>
    ///     Whether random rolls for psionics are allowed.
    ///     Guaranteed psionics will still go through.
    /// </summary>
    public static readonly CVarDef<bool> PsionicRollsEnabled =
        CVarDef.Create("psionics.rolls_enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     Whether height & width sliders adjust a character's Fixture Component
    /// </summary>
    public static readonly CVarDef<bool> HeightAdjustModifiesHitbox =
        CVarDef.Create("heightadjust.modifies_hitbox", true, CVar.SERVERONLY);

    /// <summary>
    ///     Whether height & width sliders adjust a player's max view distance
    /// </summary>
    public static readonly CVarDef<bool> HeightAdjustModifiesZoom =
        CVarDef.Create("heightadjust.modifies_zoom", false, CVar.SERVERONLY);

    /// <summary>
    ///     Enables station goals
    /// </summary>
    public static readonly CVarDef<bool> StationGoalsEnabled =
        CVarDef.Create("game.station_goals", true, CVar.SERVERONLY);

    /// <summary>
    ///     Chance for a station goal to be sent
    /// </summary>
    public static readonly CVarDef<float> StationGoalsChance =
        CVarDef.Create("game.station_goals_chance", 0.1f, CVar.SERVERONLY);

    #region Contests System

    /// <summary>
    ///     The MASTER TOGGLE for the entire Contests System.
    ///     ALL CONTESTS BELOW, regardless of type or setting will output 1f when false.
    /// </summary>
    public static readonly CVarDef<bool> DoContestsSystem =
        CVarDef.Create("contests.do_contests_system", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Contest functions normally include an optional override to bypass the clamp set by max_percentage.
    ///     This CVar disables the bypass when false, forcing all implementations to comply with max_percentage.
    /// </summary>
    public static readonly CVarDef<bool> AllowClampOverride =
        CVarDef.Create("contests.allow_clamp_override", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all MassContest functions. All mass contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoMassContests =
        CVarDef.Create("contests.do_mass_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all StaminaContest functions. All stamina contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoStaminaContests =
        CVarDef.Create("contests.do_stamina_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all HealthContest functions. All health contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoHealthContests =
        CVarDef.Create("contests.do_health_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all MindContest functions. All mind contests output 1f when false.
    ///     MindContests are not currently implemented, and are awaiting completion of the Psionic Refactor
    /// </summary>
    public static readonly CVarDef<bool> DoMindContests =
        CVarDef.Create("contests.do_mind_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     The maximum amount that Mass Contests can modify a physics multiplier, given as a +/- percentage
    ///     Default of 0.25f outputs between * 0.75f and 1.25f
    /// </summary>
    public static readonly CVarDef<float> MassContestsMaxPercentage =
        CVarDef.Create("contests.max_percentage", 0.25f, CVar.REPLICATED | CVar.SERVER);

    #endregion

    #region Supermatter System

    /// <summary>
    ///     With completely default supermatter values, Singuloose delamination will occur if engineers inject at least 900 moles of coolant per tile
    ///     in the crystal chamber. For reference, a gas canister contains 1800 moles of air. This Cvar directly multiplies the amount of moles required to singuloose.
    /// </summary>
    public static readonly CVarDef<float> SupermatterSingulooseMolesModifier =
        CVarDef.Create("supermatter.singuloose_moles_modifier", 1f, CVar.SERVER);

    /// <summary>
    ///     Toggles whether or not Singuloose delaminations can occur. If both Singuloose and Tesloose are disabled, it will always delam into a Nuke.
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoSingulooseDelam =
        CVarDef.Create("supermatter.do_singuloose", true, CVar.SERVER);

    /// <summary>
    ///     By default, Supermatter will "Tesloose" if the conditions for Singuloose are not met, and the core's power is at least 4000.
    ///     The actual reasons for being at least this amount vary by how the core was screwed up, but traditionally it's caused by "The core is on fire".
    ///     This Cvar multiplies said power threshold for the purpose of determining if the delam is a Tesloose.
    /// </summary>
    public static readonly CVarDef<float> SupermatterTesloosePowerModifier =
        CVarDef.Create("supermatter.tesloose_power_modifier", 1f, CVar.SERVER);

    /// <summary>
    ///     Toggles whether or not Tesloose delaminations can occur. If both Singuloose and Tesloose are disabled, it will always delam into a Nuke.
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoTeslooseDelam =
        CVarDef.Create("supermatter.do_tesloose", true, CVar.SERVER);

    /// <summary>
    ///     When true, bypass the normal checks to determine delam type, and instead use the type chosen by supermatter.forced_delam_type
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoForceDelam =
        CVarDef.Create("supermatter.do_force_delam", false, CVar.SERVER);

    /// <summary>
    ///     If supermatter.do_force_delam is true, this determines the delamination type, bypassing the normal checks.
    /// </summary>
    public static readonly CVarDef<DelamType> SupermatterForcedDelamType =
        CVarDef.Create("supermatter.forced_delam_type", DelamType.Singulo, CVar.SERVER);

    /// <summary>
    ///     Directly multiplies the amount of rads put out by the supermatter. Be VERY conservative with this.
    /// </summary>
    public static readonly CVarDef<float> SupermatterRadsModifier =
        CVarDef.Create("supermatter.rads_modifier", 1f, CVar.SERVER);

    #endregion
    /// <summary>
    /// Set to true to disable parallel processing in the pow3r solver.
    /// </summary>
    public static readonly CVarDef<bool> DebugPow3rDisableParallel =
        CVarDef.Create("debug.pow3r_disable_parallel", true, CVar.SERVERONLY);
}
