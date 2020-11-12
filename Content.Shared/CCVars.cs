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

        /// <summary>
        ///     When enabled, guests will be assigned permanent UIDs and will have their preferences stored.
        /// </summary>
        public static readonly CVarDef<bool>
            GamePersistGuests = CVarDef.Create("game.persistguests", true, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<int> GameSuspicionMinPlayers =
            CVarDef.Create("game.suspicion_min_players", 5);

        public static readonly CVarDef<int> GameSuspicionMinTraitors =
            CVarDef.Create("game.suspicion_min_traitors", 2);

        public static readonly CVarDef<int> GameSuspicionPlayersPerTraitor =
            CVarDef.Create("game.suspicion_players_per_traitor", 5);

        public static readonly CVarDef<int> GameSuspicionStartingBalance =
            CVarDef.Create("game.suspicion_starting_balance", 20);

        public static readonly CVarDef<bool> GameDiagonalMovement =
            CVarDef.Create("game.diagonalmovement", true, CVar.ARCHIVE);


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
            CVarDef.Create("parallax.enabled", true);

        public static readonly CVarDef<bool> ParallaxDebug =
            CVarDef.Create("parallax.debug", true);


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
    }
}
