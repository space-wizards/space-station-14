using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._EinsteinEngines.CCVar;

[CVarDefs]
public sealed partial class EinsteinCCVars : CVars
{
    // TODO: Move the rest of the announcer code to _EinsteinEngines

    // Announcers

    /// <summary>
    ///     Weighted list of announcers to choose from
    /// </summary>
    public static readonly CVarDef<string> AnnouncerList =
        CVarDef.Create("announcer.list", "RandomAnnouncers", CVar.REPLICATED);

    /// <summary>
    ///     Optionally force set an announcer
    /// </summary>
    public static readonly CVarDef<string> Announcer =
        CVarDef.Create("announcer.announcer", "", CVar.SERVERONLY);

    /// <summary>
    ///     Optionally blacklist announcers
    ///     List of IDs separated by commas
    /// </summary>
    public static readonly CVarDef<string> AnnouncerBlacklist =
        CVarDef.Create("announcer.blacklist", "", CVar.SERVERONLY);

    /// <summary>
    ///     Changes how loud the announcers are for the client
    /// </summary>
    public static readonly CVarDef<float> AnnouncerVolume =
        CVarDef.Create("announcer.volume", 0.5f, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Disables multiple announcement sounds from playing at once
    /// </summary>
    public static readonly CVarDef<bool> AnnouncerDisableMultipleSounds =
        CVarDef.Create("announcer.disable_multiple_sounds", false, CVar.ARCHIVE | CVar.CLIENTONLY);

    // Supermatter

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
    ///     Base amount of radiation that the supermatter emits.
    /// </summary>
    public static readonly CVarDef<float> SupermatterRadsBase =
        CVarDef.Create("supermatter.rads_base", 3f, CVar.SERVER);

    /// <summary>
    ///     Directly multiplies the amount of rads put out by the supermatter. Be VERY conservative with this.
    /// </summary>
    public static readonly CVarDef<float> SupermatterRadsModifier =
        CVarDef.Create("supermatter.rads_modifier", 1f, CVar.SERVER);

    /// <summary>
    ///     How often the supermatter should announce its status.
    /// </summary>
    public static readonly CVarDef<float> SupermatterYellTimer =
        CVarDef.Create("supermatter.yell_timer", 60f, CVar.SERVER);
}
