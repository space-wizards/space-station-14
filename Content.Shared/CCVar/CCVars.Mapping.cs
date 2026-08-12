using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Will mapping mode enable autosaves when it's activated?
    /// </summary>
    public static readonly CVarDef<bool>
        MappingAutosaveEnabled = CVarDef.Create("mapping.autosave", true, CVar.SERVERONLY);

    /// <summary>
    ///     Autosave interval in seconds.
    /// </summary>
    public static readonly CVarDef<float>
        MappingAutosaveInterval = CVarDef.Create("mapping.autosave_interval", 600f, CVar.SERVERONLY);

    /// <summary>
    ///     Directory in server user data to save to. Saves will be inside folders in this directory.
    /// </summary>
    public static readonly CVarDef<string>
        MappingAutosaveDirectory = CVarDef.Create("mapping.autosave_dir", "MappingAutosaves", CVar.SERVERONLY);
}
