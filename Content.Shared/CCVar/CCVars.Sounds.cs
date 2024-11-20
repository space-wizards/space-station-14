using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> LobbyMusicEnabled =
        CVarDef.Create("ambience.lobby_music_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<bool> EventMusicEnabled =
        CVarDef.Create("ambience.event_music_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Round end sound (APC Destroyed)
    /// </summary>
    public static readonly CVarDef<bool> RestartSoundsEnabled =
        CVarDef.Create("ambience.restart_sounds_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);



    public static readonly CVarDef<bool> AdminSoundsEnabled =
        CVarDef.Create("audio.admin_sounds_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<string> AdminChatSoundPath =
        CVarDef.Create("audio.admin_chat_sound_path",
            "/Audio/Items/pop.ogg",
            CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    public static readonly CVarDef<float> AdminChatSoundVolume =
        CVarDef.Create("audio.admin_chat_sound_volume", -5f, CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    public static readonly CVarDef<string> AHelpSound =
        CVarDef.Create("audio.ahelp_sound", "/Audio/Effects/adminhelp.ogg", CVar.ARCHIVE | CVar.CLIENTONLY);
}
