using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    // These are server-only for now since I don't foresee a client use yet,
    // and I don't wanna have to start coming up with like .client suffixes and stuff like that.

    /// <summary>
    ///     Configuration presets to load during startup.
    ///     Multiple presets can be separated by comma and are loaded in order.
    /// </summary>
    /// <remarks>
    ///     Loaded presets must be located under the <c>ConfigPresets/</c> resource directory and end with the <c>.toml</c> extension.
    ///     Only the file name (without extension) must be given for this variable.
    /// </remarks>
    public static readonly CVarDef<string> ConfigPresets =
        CVarDef.Create("config.presets", "", CVar.SERVERONLY);

    /// <summary>
    ///     Whether to load the preset development CVars.
    ///     This disables some things like lobby to make development easier.
    ///     Even when true, these are only loaded if the game is compiled with <c>DEVELOPMENT</c> set.
    /// </summary>
    public static readonly CVarDef<bool> ConfigPresetDevelopment =
        CVarDef.Create("config.preset_development", true, CVar.SERVERONLY);

    /// <summary>
    ///     Whether to load the preset debug CVars.
    ///     Even when true, these are only loaded if the game is compiled with <c>DEBUG</c> set.
    /// </summary>
    public static readonly CVarDef<bool> ConfigPresetDebug =
        CVarDef.Create("config.preset_debug", true, CVar.SERVERONLY);
}
