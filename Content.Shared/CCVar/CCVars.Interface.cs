using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// The sound played when clicking a UI button
    /// </summary>
    public static readonly CVarDef<string> UIClickSound =
        CVarDef.Create("interface.click_sound", "/Audio/UserInterface/click.ogg", CVar.REPLICATED);

    /// <summary>
    /// The sound played when the mouse hovers over a clickable UI element
    /// </summary>
    public static readonly CVarDef<string> UIHoverSound =
        CVarDef.Create("interface.hover_sound", "/Audio/UserInterface/hover.ogg", CVar.REPLICATED);

    /// <summary>
    /// The layout style of the UI
    /// </summary>
    public static readonly CVarDef<string> UILayout =
        CVarDef.Create("ui.layout", "Default", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// The dimensions for the chat window in Default UI mode
    /// </summary>
    public static readonly CVarDef<string> DefaultScreenChatSize =
        CVarDef.Create("ui.default_chat_size", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// The width of the chat panel in Separated UI mode
    /// </summary>
    public static readonly CVarDef<string> SeparatedScreenChatSize =
        CVarDef.Create("ui.separated_chat_size", "0.6,0", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> OutlineEnabled =
        CVarDef.Create("outline.enabled", true, CVar.CLIENTONLY);

    /// <summary>
    /// Determines how antagonist status/roletype is displayed. Based on AdminOverlayAntagFormats enum
    /// Binary: Roletypes of interest get an "ANTAG" label
    /// Roletype: Roletypes of interest will have their roletype name displayed in their specific color
    /// Subtype: Roletypes of interest will have their subtype displayed. if subtype is not set, roletype will be shown.
    /// </summary>
    public static readonly CVarDef<string> AdminOverlayAntagFormat =
        CVarDef.Create("ui.admin_overlay_antag_format", "Subtype", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, the admin overlay will display the total time of the players
    /// </summary>
    public static readonly CVarDef<bool> AdminOverlayPlaytime =
        CVarDef.Create("ui.admin_overlay_playtime", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, the admin overlay will display the player's starting role.
    /// </summary>
    public static readonly CVarDef<bool> AdminOverlayStartingJob =
        CVarDef.Create("ui.admin_overlay_starting_job", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Determines how antagonist status/roletype is displayed Before character names on the Player Tab
    /// Off: No symbol is shown.
    /// Basic: The same antag symbol is shown for anyone marked as antag.
    /// Specific: The roletype-specific symbol is shown for anyone marked as antag.
    /// </summary>
    public static readonly CVarDef<string> AdminPlayerTabSymbolSetting =
        CVarDef.Create("ui.admin_player_tab_symbols", "Specific", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Determines what columns are colorized
    /// Off: None.
    /// Character: The character names of "roletypes-of-interest" have their role type's color.
    /// Roletype: Role types are shown in their respective colors.
    /// Both: Both characters and role types are colorized.
    /// </summary>
    public static readonly CVarDef<string> AdminPlayerTabColorSetting =
        CVarDef.Create("ui.admin_player_tab_color", "Both", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Determines what's displayed in the Role column - role type, subtype, or both.
    /// RoleType
    /// SubType
    /// RoleTypeSubtype
    /// SubtypeRoleType
    /// </summary>
    public static readonly CVarDef<string> AdminPlayerTabRoleSetting =
        CVarDef.Create("ui.admin_player_tab_role", "Subtype", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Determines how antagonist status/roletype is displayed. Based on AdminOverlayAntagSymbolStyles enum
    /// Off: No symbol is shown.
    /// Basic: The same antag symbol is shown for anyone marked as antag.
    /// Specific: The roletype-specific symbol is shown for anyone marked as antag.
    /// </summary>
    public static readonly CVarDef<string> AdminOverlaySymbolStyle =
        CVarDef.Create("ui.admin_overlay_symbol_style", "Specific", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// The range (in tiles) around the cursor within which the admin overlays of ghosts start to fade out
    /// </summary>
    public static readonly CVarDef<int> AdminOverlayGhostFadeDistance =
        CVarDef.Create("ui.admin_overlay_ghost_fade_distance", 6, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// The range (in tiles) around the cursor within which the admin overlays of ghosts disappear
    /// </summary>
    public static readonly CVarDef<int> AdminOverlayGhostHideDistance =
        CVarDef.Create("ui.admin_overlay_ghost_hide_distance", 2, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// The maximum range (in tiles) at which admin overlay entries still merge to form a stack
    /// Recommended to keep under 1, otherwise the overlays of people sitting next to each other will stack
    /// </summary>
    public static readonly CVarDef<float> AdminOverlayMergeDistance =
        CVarDef.Create("ui.admin_overlay_merge_distance", 0.33f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// The maximum size that an overlay stack can reach. Additional overlays will be superimposed over the last one.
    /// </summary>
    public static readonly CVarDef<int> AdminOverlayStackMax =
        CVarDef.Create("ui.admin_overlay_stack_max", 3, CVar.CLIENTONLY | CVar.ARCHIVE);
}
