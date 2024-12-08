using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<string> UIClickSound =
        CVarDef.Create("interface.click_sound", "/Audio/UserInterface/click.ogg", CVar.REPLICATED);

    public static readonly CVarDef<string> UIHoverSound =
        CVarDef.Create("interface.hover_sound", "/Audio/UserInterface/hover.ogg", CVar.REPLICATED);

    public static readonly CVarDef<string> UILayout =
        CVarDef.Create("ui.layout", "Default", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> DefaultScreenChatSize =
        CVarDef.Create("ui.default_chat_size", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> SeparatedScreenChatSize =
        CVarDef.Create("ui.separated_chat_size", "0.6,0", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> OutlineEnabled =
        CVarDef.Create("outline.enabled", true, CVar.CLIENTONLY);

    // <summary>
    //  The width of the right side (chat) panel in the lobby
    // </summary>
    public static readonly CVarDef<int> ServerLobbyRightPanelWidth =
        CVarDef.Create("server.lobby_right_panel_width", 650, CVar.REPLICATED | CVar.SERVER);
}
