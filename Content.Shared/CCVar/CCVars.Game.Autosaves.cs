using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     If enabled, will automatically save the game state every
    ///     <see cref="AutoSavesInterval"/> minutes into <see cref="AutoSavesDirectory"/>.
    /// </summary>
    public static readonly CVarDef<bool>
        AutoSavesEnabled = CVarDef.Create("game.autosave", false, CVar.SERVERONLY); // TODO SERIALIZATION when saving the game state doesn't take forever, enable this

    /// <summary>
    ///     Game state Autosave interval in minutes. Does nothing when <see cref="AutoSavesEnabled"/> is set to false.
    /// </summary>
    public static readonly CVarDef<int>
        AutoSavesInterval = CVarDef.Create("game.autosave_interval", 3, CVar.SERVERONLY);

    /// <summary>
    ///     Interval between the first message and the autosave in minutes.
    /// </summary>
    public static readonly CVarDef<int>
        AutoSavesMessageIntervalFirst = CVarDef.Create("game.autosave_message1_interval", 2, CVar.SERVERONLY);

    /// <summary>
    ///     Interval between the second message and the autosave in minutes.
    /// </summary>
    public static readonly CVarDef<int>
        AutoSavesMessageIntervalSecond = CVarDef.Create("game.autosave_message2_interval", 1, CVar.SERVERONLY);

    /// <summary>
    ///     Directory in server user data to save the full game state into.
    /// </summary>
    public static readonly CVarDef<string>
        AutoSavesDirectory = CVarDef.Create("game.autosave_dir", "AutoSaves", CVar.SERVERONLY);
}
