using Robust.Shared.Configuration;

namespace Content.Shared.DeadSpace.CCCCVars;

/// <summary>
///     DeadSpace modules console variables
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming
public sealed class CCCCVars
{
    /*
	* GCF
	*/

    /// <summary>
    ///     Whether GCF being shown is enabled at all.
    /// </summary>

    public static readonly CVarDef<bool> GCFEnabled =
        CVarDef.Create("gcf_auto.enabled", false);

    /// <summary>
    ///     Notify for admin about GCF Clean.
    /// </summary>
    public static readonly CVarDef<bool> GCFNotify =
        CVarDef.Create("gcf_auto.notify", false);

    /// <summary>
    ///     The number of seconds between each GCF
    /// </summary>
    public static readonly CVarDef<float> GCFFrequency =
        CVarDef.Create("gcf_auto.frequency", 300f);

    /*
	* InfoLinks
	*/

    /// <summary>
    /// IPs address for reconnect.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksIPs =
        CVarDef.Create("infolinks.ips", string.Empty, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Multiplier for playtime.
    /// </summary>
    public static readonly CVarDef<float> PlayTimeMultiplier =
        CVarDef.Create("playtime.multiplier", 1f, CVar.SERVER | CVar.REPLICATED);

    /*
	* TTS
	*/

    public static readonly CVarDef<float> TTSVolumeRadio =
        CVarDef.Create("tts.volume_radio", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RadioTTSSoundsEnabled =
        CVarDef.Create("audio.radio_tts_sounds_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
	* Jukebox
	*/

    public static readonly CVarDef<float> JukeboxMusicVolume =
        CVarDef.Create("jukebox.volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> JukeboxAutoVolume =
        CVarDef.Create("jukebox.auto_volume", 0.5f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Alert Level
     */

    public static readonly CVarDef<float> AlertLevelVolume =
        CVarDef.Create("audio.alert_level_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Annocment
     */
    public static readonly CVarDef<float> AnnonceVolume =
        CVarDef.Create("audio.annonce_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Annocment
     */
    public static readonly CVarDef<float> AdminVolume =
        CVarDef.Create("audio.admin_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Item sounds
     */

    public static readonly CVarDef<float> ItemSoundsVolume =
        CVarDef.Create("audio.item_sounds_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Boss music
     */

    public static readonly CVarDef<bool> BossMusicEnabled =
        CVarDef.Create("audio.boss_music_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BossMusicVolume =
        CVarDef.Create("audio.boss_music_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
    * Taipan
    */

    /// <summary>
    /// Should Taipan spawn or not.
    /// </summary>
    public static readonly CVarDef<bool> TaipanEnabled =
        CVarDef.Create("taipan.enabled", false, CVar.SERVERONLY);

    /*
    * Lavaland
    */

    /// <summary>
    /// Should stations auto-generate Lavaland on round start.
    /// </summary>
    public static readonly CVarDef<bool> LavalandAutoGenerate =
        CVarDef.Create("lavaland.auto_generate", true, CVar.SERVERONLY);

    /// <summary>
    /// Moves long-stuck dynamic physics bodies out of static hard overlaps.
    /// </summary>
    public static readonly CVarDef<bool> PhysicsSanityEnabled =
        CVarDef.Create("physics.sanity_enabled", true, CVar.SERVERONLY);

    /*
    * Lobby ui
    */

    /// <summary>
    /// Lobby default background. Can be Parallax or Image.
    /// </summary>
    public static readonly CVarDef<string> Background =
        CVarDef.Create("ui.background", "Image", CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
    * Player Count Mode
    */

    /// <summary>
    /// Whether to use total players or ready players for game mode selection.
    /// </summary>
    public static readonly CVarDef<bool> GameModesUseTotalPlayers =
        CVarDef.Create("game.modes_use_total_players", true, CVar.SERVERONLY | CVar.ARCHIVE);
    /*
    * SysNotify
    */

    /// <summary>
    /// Dictionary for ping
    /// </summary>
    public static readonly CVarDef<string> SysNotifyCvar =
        CVarDef.Create("sysnotify.Dict", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// How much we were wating to next ping
    /// </summary>
    public static readonly CVarDef<int> SysNotifyCoolDown =
        CVarDef.Create("sysnotify.cooldown", 1, CVar.CLIENTONLY | CVar.ARCHIVE);
    /// <summary>
    /// Get ping or no
    /// </summary>
    public static readonly CVarDef<bool> SysNotifyPerm =
        CVarDef.Create("sysnotify.permission", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> SysNotifySoundPath =
        CVarDef.Create("sysnotifys.soundpath", "/Audio/Effects/balloon-pop.ogg", CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
    * Storage
    */

    /// <summary>
    ///     Allows opening multiple inventory/storage windows simultaneously.
    /// </summary>
    public static readonly CVarDef<bool> MultipleInventoryWindows =
        CVarDef.Create("storage.multiple_inventory_windows", false, CVar.CLIENTONLY | CVar.ARCHIVE);
    public static readonly CVarDef<int> MaxBroadcastLength =
        CVarDef.Create("chat.max_broadcast_length", 10, CVar.SERVER | CVar.REPLICATED);

    /*
    * Попауты
    */
    public static readonly CVarDef<bool> PopOutChat =
    CVarDef.Create("Chat.PopOut", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
