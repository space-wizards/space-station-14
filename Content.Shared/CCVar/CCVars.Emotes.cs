using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Global emote (number 1) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote1 =
        CVarDef.Create("emotes.global_custom_1", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Global emote (number 2) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote2 =
        CVarDef.Create("emotes.global_custom_2", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Global emote (number 3) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote3 =
        CVarDef.Create("emotes.global_custom_3", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Global emote (number 4) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote4 =
        CVarDef.Create("emotes.global_custom_4", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Global emote (number 5) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote5 =
        CVarDef.Create("emotes.global_custom_5", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Global emote (number 6) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote6 =
        CVarDef.Create("emotes.global_custom_6", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Global emote (number 7) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote7 =
        CVarDef.Create("emotes.global_custom_7", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Global emote (number 8) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote8 =
        CVarDef.Create("emotes.global_custom_8", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Global emote (number 9) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote9 =
        CVarDef.Create("emotes.global_custom_9", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Global emote (number 10) available for all player characters.
    /// </summary>
    public static readonly CVarDef<string> GlobalCustomEmote10 =
        CVarDef.Create("emotes.global_custom_10", defaultValue:"", CVar.CLIENTONLY | CVar.ARCHIVE);
}
