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
    /// If true, the admin overlay will be displayed in the old style (showing only "ANTAG")
    /// </summary>
    public static readonly CVarDef<bool> AdminOverlayClassic =
        CVarDef.Create("ui.admin_overlay_classic", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, the admin overlay will display the total time of the players
    /// </summary>
    public static readonly CVarDef<bool> AdminOverlayPlaytime =
        CVarDef.Create("ui.admin_overlay_playtime", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, the admin overlay will display the players starting position.
    /// </summary>
    public static readonly CVarDef<bool> AdminOverlayStartingJob =
        CVarDef.Create("ui.admin_overlay_starting_job", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, the admin window player tab will show different antag symbols for each role type
    /// </summary>
    public static readonly CVarDef<bool> AdminPlayerlistSeparateSymbols =
        CVarDef.Create("ui.admin_playerlist_separate_symbols", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, characters with antag role types will have their names colored by their role type
    /// </summary>
    public static readonly CVarDef<bool> AdminPlayerlistHighlightedCharacterColor =
        CVarDef.Create("ui.admin_playerlist_highlighted_character_color", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, the Role Types column will be colored
    /// </summary>
    public static readonly CVarDef<bool> AdminPlayerlistRoleTypeColor =
        CVarDef.Create("ui.admin_playerlist_role_type_color", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, the admin overlay will show antag symbols
    /// </summary>
    public static readonly CVarDef<bool> AdminOverlaySymbols =
        CVarDef.Create("ui.admin_overlay_symbols", true, CVar.CLIENTONLY | CVar.ARCHIVE);

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
