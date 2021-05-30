#nullable enable
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared
{
    // ReSharper disable once InconsistentNaming
    [CVarDefs]
    public sealed class CCVars : CVars
    {
        /*
         * Status
         */

        public static readonly CVarDef<string> StatusMoMMIUrl =
            CVarDef.Create("status.mommiurl", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> StatusMoMMIPassword =
            CVarDef.Create("status.mommipassword", "", CVar.SERVERONLY);


        /*
         * Game
         */

        public static readonly CVarDef<bool>
            EventsEnabled = CVarDef.Create("events.enabled", false, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool>
            GameLobbyEnabled = CVarDef.Create("game.lobbyenabled", false, CVar.ARCHIVE);

        public static readonly CVarDef<int>
            GameLobbyDuration = CVarDef.Create("game.lobbyduration", 60, CVar.ARCHIVE);

        public static readonly CVarDef<string>
            GameLobbyDefaultPreset = CVarDef.Create("game.defaultpreset", "Suspicion", CVar.ARCHIVE);

        public static readonly CVarDef<bool>
            GameLobbyFallbackEnabled = CVarDef.Create("game.fallbackenabled", true, CVar.ARCHIVE);

        public static readonly CVarDef<string>
            GameLobbyFallbackPreset = CVarDef.Create("game.fallbackpreset", "Sandbox", CVar.ARCHIVE);

        public static readonly CVarDef<bool>
            GameLobbyEnableWin = CVarDef.Create("game.enablewin", true, CVar.ARCHIVE);

        public static readonly CVarDef<int>
            GameMaxCharacterSlots = CVarDef.Create("game.maxcharacterslots", 10, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<string>
            GameMap = CVarDef.Create("game.map", "Maps/saltern.yml", CVar.SERVERONLY);

        /// <summary>
        ///     When enabled, guests will be assigned permanent UIDs and will have their preferences stored.
        /// </summary>
        public static readonly CVarDef<bool>
            GamePersistGuests = CVarDef.Create("game.persistguests", true, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool> GameDiagonalMovement =
            CVarDef.Create("game.diagonalmovement", true, CVar.ARCHIVE);

        /*
         * Suspicion
         */

        public static readonly CVarDef<int> SuspicionMinPlayers =
            CVarDef.Create("suspicion.min_players", 5);

        public static readonly CVarDef<int> SuspicionMinTraitors =
            CVarDef.Create("suspicion.min_traitors", 2);

        public static readonly CVarDef<int> SuspicionPlayersPerTraitor =
            CVarDef.Create("suspicion.players_per_traitor", 5);

        public static readonly CVarDef<int> SuspicionStartingBalance =
            CVarDef.Create("suspicion.starting_balance", 20);

        public static readonly CVarDef<int> SuspicionMaxTimeSeconds =
            CVarDef.Create("suspicion.max_time_seconds", 300);

        /*
         * Traitor
         */

        public static readonly CVarDef<int> TraitorMinPlayers =
            CVarDef.Create("traitor.min_players", 5);

        public static readonly CVarDef<int> TraitorMaxTraitors =
            CVarDef.Create("traitor.max_traitors", 4);

        public static readonly CVarDef<int> TraitorPlayersPerTraitor =
            CVarDef.Create("traitor.players_per_traitor", 5);

        public static readonly CVarDef<int> TraitorCodewordCount =
            CVarDef.Create("traitor.codeword_count", 4);

        public static readonly CVarDef<int> TraitorStartingBalance =
            CVarDef.Create("traitor.starting_balance", 20);

        public static readonly CVarDef<int> TraitorMaxDifficulty =
            CVarDef.Create("traitor.max_difficulty", 4);

        public static readonly CVarDef<int> TraitorMaxPicks =
            CVarDef.Create("traitor.max_picks", 20);

        /*
         * TraitorDeathMatch
         */

        public static readonly CVarDef<int> TraitorDeathMatchStartingBalance =
            CVarDef.Create("traitordm.starting_balance", 20);

        /*
         * Console
         */

        public static readonly CVarDef<bool>
            ConsoleLoginLocal = CVarDef.Create("console.loginlocal", true, CVar.ARCHIVE | CVar.SERVERONLY);


        /*
         * Database stuff
         */

        public static readonly CVarDef<string> DatabaseEngine =
            CVarDef.Create("database.engine", "sqlite", CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabaseSqliteDbPath =
            CVarDef.Create("database.sqlite_dbpath", "preferences.db", CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabasePgHost =
            CVarDef.Create("database.pg_host", "localhost", CVar.SERVERONLY);

        public static readonly CVarDef<int> DatabasePgPort =
            CVarDef.Create("database.pg_port", 5432, CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabasePgDatabase =
            CVarDef.Create("database.pg_database", "ss14", CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabasePgUsername =
            CVarDef.Create("database.pg_username", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabasePgPassword =
            CVarDef.Create("database.pg_password", "", CVar.SERVERONLY);

        // Basically only exists for integration tests to avoid race conditions.
        public static readonly CVarDef<bool> DatabaseSynchronous =
            CVarDef.Create("database.sync", false, CVar.SERVERONLY);


        /*
         * Outline
         */

        public static readonly CVarDef<bool> OutlineEnabled =
            CVarDef.Create("outline.enabled", true, CVar.CLIENTONLY);


        /*
         * Parallax
         */

        public static readonly CVarDef<bool> ParallaxEnabled =
            CVarDef.Create("parallax.enabled", true, CVar.CLIENTONLY);

        public static readonly CVarDef<bool> ParallaxDebug =
            CVarDef.Create("parallax.debug", false, CVar.CLIENTONLY);

        /*
         * Physics
         */

        public static readonly CVarDef<float> TileFrictionModifier =
            CVarDef.Create("physics.tilefriction", 15.0f);

        public static readonly CVarDef<float> StopSpeed =
            CVarDef.Create("physics.stopspeed", 0.1f);

        /*
         * Ambience
         */

        public static readonly CVarDef<bool> AmbienceBasicEnabled =
            CVarDef.Create("ambience.basicenabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * Lobby music
         */

        public static readonly CVarDef<bool> LobbyMusicEnabled =
            CVarDef.Create("ambience.lobbymusicenabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * HUD
         */

        public static readonly CVarDef<int> HudTheme =
            CVarDef.Create("hud.theme", 0, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * AI
         */

        public static readonly CVarDef<int> AIMaxUpdates =
            CVarDef.Create("ai.maxupdates", 64);


        /*
         * Net
         */

        public static readonly CVarDef<float> NetAtmosDebugOverlayTickRate =
            CVarDef.Create("net.atmosdbgoverlaytickrate", 3.0f);

        public static readonly CVarDef<float> NetGasOverlayTickRate =
            CVarDef.Create("net.gasoverlaytickrate", 3.0f);

        /*
         * Admin stuff
         */

        public static readonly CVarDef<bool> AdminAnnounceLogin =
            CVarDef.Create("admin.announce_login", true, CVar.SERVERONLY);

        public static readonly CVarDef<bool> AdminAnnounceLogout =
            CVarDef.Create("admin.announce_logout", true, CVar.SERVERONLY);

        /*
         * Atmos
         */

        /// <summary>
        ///     Whether gas differences will move entities.
        /// </summary>
        public static readonly CVarDef<bool> SpaceWind =
            CVarDef.Create("atmos.space_wind", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether monstermos tile equalization is enabled.
        /// </summary>
        public static readonly CVarDef<bool> MonstermosEqualization =
            CVarDef.Create("atmos.monstermos_equalization", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether atmos superconduction is enabled.
        /// </summary>
        /// <remarks> Disabled by default, superconduction is awful. </remarks>
        public static readonly CVarDef<bool> Superconduction =
            CVarDef.Create("atmos.superconduction", false, CVar.SERVERONLY);

        /// <summary>
        ///     Maximum time in milliseconds that atmos can take processing.
        /// </summary>
        public static readonly CVarDef<float> AtmosMaxProcessTime =
            CVarDef.Create("atmos.max_process_time", 5f, CVar.SERVERONLY);

        /// <summary>
        ///     Atmos tickrate in TPS. Atmos processing will happen every 1/TPS seconds.
        /// </summary>
        public static readonly CVarDef<float> AtmosTickRate =
            CVarDef.Create("atmos.tickrate", 26f, CVar.SERVERONLY);

        public static readonly CVarDef<bool> ExcitedGroupsSpaceIsAllConsuming =
            CVarDef.Create("atmos.excited_groups_space_is_all_consuming", false, CVar.SERVERONLY);

        /*
         * MIDI instruments
         */

        public static readonly CVarDef<int> MaxMidiEventsPerSecond =
            CVarDef.Create("midi.max_events_per_second", 1000, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> MaxMidiEventsPerBatch =
            CVarDef.Create("midi.max_events_per_batch", 60, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> MaxMidiBatchesDropped =
            CVarDef.Create("midi.max_batches_dropped", 1, CVar.SERVERONLY);

        public static readonly CVarDef<int> MaxMidiLaggedBatches =
            CVarDef.Create("midi.max_lagged_batches", 8, CVar.SERVERONLY);

        /*
         * Holidays
         */

        public static readonly CVarDef<bool> HolidaysEnabled = CVarDef.Create("holidays.enabled", true, CVar.SERVERONLY);

        /*
         * Branding stuff
         */

        public static readonly CVarDef<bool> BrandingSteam = CVarDef.Create("branding.steam", false, CVar.CLIENTONLY);

        /*
         * OOC
         */

        public static readonly CVarDef<bool> OocEnabled = CVarDef.Create("ooc.enabled", true, CVar.NOTIFY);

        public static readonly CVarDef<bool> AdminOocEnabled =
            CVarDef.Create("ooc.enabled_admin", true, CVar.NOTIFY);

        /*
         * Context Menu Grouping Types
         */
        public static readonly CVarDef<int> ContextMenuGroupingType = CVarDef.Create("context_menu", 0, CVar.CLIENTONLY);

        /*
         * VOTE
         */

        public static readonly CVarDef<float> VoteRestartRequiredRatio =
            CVarDef.Create("vote.restart_required_ratio", 0.8f, CVar.SERVERONLY);

        /*
         * BAN
         */

        public static readonly CVarDef<bool> BanHardwareIds =
            CVarDef.Create("ban.hardware_ids", false, CVar.SERVERONLY);
        /*
         * VIEWPORT
         */

        public static readonly CVarDef<bool> ViewportStretch =
            CVarDef.Create("viewport.stretch", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> ViewportFixedScaleFactor =
            CVarDef.Create("viewport.fixed_scale_factor", 2, CVar.CLIENTONLY | CVar.ARCHIVE);

        // This default is basically specifically chosen so fullscreen/maximized 1080p hits a 2x snap and does NN.
        public static readonly CVarDef<int> ViewportSnapToleranceMargin =
            CVarDef.Create("viewport.snap_tolerance_margin", 64, CVar.CLIENTONLY);

        public static readonly CVarDef<int> ViewportSnapToleranceClip =
            CVarDef.Create("viewport.snap_tolerance_clip", 32, CVar.CLIENTONLY);

        public static readonly CVarDef<bool> ViewportScaleRender =
            CVarDef.Create("viewport.scale_render", true, CVar.CLIENTONLY | CVar.ARCHIVE);
    }
}
