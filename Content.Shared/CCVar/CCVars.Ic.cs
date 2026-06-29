using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Restricts IC character names to alphanumeric chars.
    /// </summary>
    public static readonly CVarDef<bool> RestrictedNames =
        CVarDef.Create("ic.restricted_names", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum IC name length.
    /// </summary>
    public static readonly CVarDef<int> MaxNameLength =
        CVarDef.Create("ic.name_length", 32, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum name length for a loadout name (e.g. cyborg name).
    /// </summary>
    public static readonly CVarDef<int> MaxLoadoutNameLength =
        CVarDef.Create("ic.loadout_name_length", 32, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Allows flavor text (character descriptions).
    /// </summary>
    public static readonly CVarDef<bool> FlavorText =
        CVarDef.Create("ic.flavor_text", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum length for flavor text (character descriptions).
    /// </summary>
    public static readonly CVarDef<int> MaxFlavorTextLength =
        CVarDef.Create("ic.flavor_text_length", 512, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Sets the maximum character length of a job on an ID.
    /// </summary>
    public static readonly CVarDef<int> MaxIdJobLength =
        CVarDef.Create("ic.id_job_length", 30, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Adds a period at the end of a sentence if the sentence ends in a letter.
    /// </summary>
    public static readonly CVarDef<bool> ChatPunctuation =
        CVarDef.Create("ic.punctuation", false, CVar.SERVER);

    /// <summary>
    ///     Enables automatically forcing IC name rules. Uppercases the first letter of the first and last words of the name
    /// </summary>
    public static readonly CVarDef<bool> ICNameCase =
        CVarDef.Create("ic.name_case", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Whether or not players' characters are randomly generated rather than using their selected characters in the creator.
    /// </summary>
    public static readonly CVarDef<bool> ICRandomCharacters =
        CVarDef.Create("ic.random_characters", false, CVar.SERVER);

    /// <summary>
    ///     A weighted random prototype used to determine the species selected for random characters.
    ///     If blank, will use a round start species picked at random.
    /// </summary>
    public static readonly CVarDef<string> ICRandomSpeciesWeights =
        CVarDef.Create("ic.random_species_weights", "SpeciesWeights", CVar.SERVER);

    /// <summary>
    ///     The list of species that will NOT be given to new account joins when they are assigned a random character.
    ///     This only affects the first time a character is made for an account, nothing else.
    /// </summary>
    public static readonly CVarDef<string> ICNewAccountSpeciesBlacklist =
        CVarDef.Create("ic.blacklist_species_new_account", "Diona,Vulpkanin,Vox,SlimePerson", CVar.SERVER);

    /// <summary>
    ///     Control displaying SSD indicators near players
    /// </summary>
    public static readonly CVarDef<bool> ICShowSSDIndicator =
        CVarDef.Create("ic.show_ssd_indicator", true, CVar.CLIENTONLY);

    /// <summary>
    ///     Forces SSD characters to sleep after ICSSDSleepTime seconds
    /// </summary>
    public static readonly CVarDef<bool> ICSSDSleep =
        CVarDef.Create("ic.ssd_sleep", true, CVar.SERVER);

    /// <summary>
    ///     Time between character getting SSD status and falling asleep
    ///     Won't work without ICSSDSleep
    /// </summary>
    public static readonly CVarDef<float> ICSSDSleepTime =
        CVarDef.Create("ic.ssd_sleep_time", 600f, CVar.SERVER);
}
