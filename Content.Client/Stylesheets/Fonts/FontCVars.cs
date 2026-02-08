using Robust.Shared.Configuration;

namespace Content.Client.Stylesheets.Fonts;

// Look at me! This isn't part of CCVars!

/// <summary>
/// CVar definitions for client font preferences.
/// </summary>
/// <remarks>
/// These should generally be accessed through <see cref="FontSelectionManager"/>.
/// </remarks>
[CVarDefs]
public static class FontCVars
{
    /// <summary>
    /// The user-selected font family name for <see cref="StandardFontType.Main"/>.
    /// </summary>
    public static readonly CVarDef<string> MainFamilyName =
        CVarDef.Create<string>("font.main_family_name", "", CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// The user-selected font scale for <see cref="StandardFontType.Main"/>.
    /// </summary>
    public static readonly CVarDef<float> MainScale =
        CVarDef.Create<float>("font.main_scale", 1, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// The user-selected font family name for <see cref="StandardFontType.Title"/>.
    /// </summary>
    public static readonly CVarDef<string> TitleFamilyName =
        CVarDef.Create<string>("font.title_family_name", "", CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// The user-selected font scale for <see cref="StandardFontType.Title"/>.
    /// </summary>
    public static readonly CVarDef<float> TitleScale =
        CVarDef.Create<float>("font.title_scale", 1, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// The user-selected font family name for <see cref="StandardFontType.MachineTitle"/>.
    /// </summary>
    public static readonly CVarDef<string> MachineTitleFamilyName =
        CVarDef.Create<string>("font.machine_title_family_name", "", CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// The user-selected font scale for <see cref="StandardFontType.MachineTitle"/>.
    /// </summary>
    public static readonly CVarDef<float> MachineTitleScale =
        CVarDef.Create<float>("font.machine_title_scale", 1, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// The user-selected font family name for <see cref="StandardFontType.Monospace"/>.
    /// </summary>
    public static readonly CVarDef<string> MonospaceFamilyName =
        CVarDef.Create<string>("font.monospace_family_name", "", CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// The user-selected font scale for <see cref="StandardFontType.Monospace"/>.
    /// </summary>
    public static readonly CVarDef<float> MonospaceScale =
        CVarDef.Create<float>("font.monospace_scale", 1, CVar.ARCHIVE | CVar.CLIENTONLY);
}
