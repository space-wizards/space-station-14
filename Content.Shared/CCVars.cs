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
            CVarDef.Create<string>("status.mommiurl", null);

        public static readonly CVarDef<string> StatusMoMMIPassword =
            CVarDef.Create<string>("status.mommipassword", null);


        /*
         * Game
         */

        public static readonly CVarDef<bool>
            EventsEnabled = CVarDef.Create("events.enabled", false, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool>
            GameLobbyEnabled = CVarDef.Create("game.lobbyenabled", false, CVar.ARCHIVE);

        public static readonly CVarDef<int>
            GameLobbyDuration = CVarDef.Create("game.lobbyduration", 20, CVar.ARCHIVE);

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
         * Branding stuff
         */

        public static readonly CVarDef<bool> BrandingSteam = CVarDef.Create("branding.steam", false, CVar.CLIENTONLY);
    }
}
