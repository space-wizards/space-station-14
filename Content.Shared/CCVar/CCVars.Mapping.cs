using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Will mapping mode enable autosaves when it's activated?
    /// </summary>
    public static readonly CVarDef<bool>
        AutosaveEnabled = CVarDef.Create("mapping.autosave", true, CVar.SERVERONLY);

    /// <summary>
    ///     Autosave interval in seconds.
    /// </summary>
    public static readonly CVarDef<float>
        AutosaveInterval = CVarDef.Create("mapping.autosave_interval", 600f, CVar.SERVERONLY);

    /// <summary>
    ///     Directory in server user data to save to. Saves will be inside folders in this directory.
    /// </summary>
    public static readonly CVarDef<string>
        AutosaveDirectory = CVarDef.Create("mapping.autosave_dir", "Autosaves", CVar.SERVERONLY);
}
