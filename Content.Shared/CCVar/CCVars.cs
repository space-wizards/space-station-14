using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar
{
    // ReSharper disable once InconsistentNaming
    [CVarDefs]
    public sealed class CCVars : CVars
    {
        /*
         * Server
         */

        /// <summary>
        ///     Change this to have the changelog and rules "last seen" date stored separately.
        /// </summary>
        public static readonly CVarDef<string> ServerId =
            CVarDef.Create("server.id", "unknown_server_id", CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        ///     Name of the rules txt file in the "Resources/Server Info" dir. Include the extension.
        /// </summary>
        public static readonly CVarDef<string> RulesFile =
            CVarDef.Create("server.rules_file", "Rules.txt", CVar.REPLICATED | CVar.SERVER);

        /*
         * Ambience
         */

        /// <summary>
        /// How long we'll wait until re-sampling nearby objects for ambience. Should be pretty fast, but doesn't have to match the tick rate.
        /// </summary>
        public static readonly CVarDef<float> AmbientCooldown =
            CVarDef.Create("ambience.cooldown", 0.1f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// How large of a range to sample for ambience.
        /// </summary>
        public static readonly CVarDef<float> AmbientRange =
            CVarDef.Create("ambience.range", 5f, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Maximum simultaneous ambient sounds.
        /// </summary>
        public static readonly CVarDef<int> MaxAmbientSources =
            CVarDef.Create("ambience.max_sounds", 16, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// The minimum value the user can set for ambience.max_sounds
        /// </summary>
        public static readonly CVarDef<int> MinMaxAmbientSourcesConfigured =
            CVarDef.Create("ambience.min_max_sounds_configured", 16, CVar.REPLICATED | CVar.SERVER | CVar.CHEAT);

        /// <summary>
        /// The maximum value the user can set for ambience.max_sounds
        /// </summary>
        public static readonly CVarDef<int> MaxMaxAmbientSourcesConfigured =
            CVarDef.Create("ambience.max_max_sounds_configured", 64, CVar.REPLICATED | CVar.SERVER | CVar.CHEAT);

        /// <summary>
        /// Ambience volume.
        /// </summary>
        public static readonly CVarDef<float> AmbienceVolume =
            CVarDef.Create("ambience.volume", 0.0f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// Whether to play the station ambience (humming) sound
        /// </summary>
        public static readonly CVarDef<bool> StationAmbienceEnabled =
            CVarDef.Create("ambience.station_ambience", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// Whether to play the space ambience
        /// </summary>
        public static readonly CVarDef<bool> SpaceAmbienceEnabled =
            CVarDef.Create("ambience.space_ambience", true, CVar.ARCHIVE | CVar.CLIENTONLY);

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

        /// <summary>
        ///     Controls if the game should run station events
        /// </summary>
        public static readonly CVarDef<bool>
            EventsEnabled = CVarDef.Create("events.enabled", true, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        ///     Disables most functionality in the GameTicker.
        /// </summary>
        public static readonly CVarDef<bool>
            GameDummyTicker = CVarDef.Create("game.dummyticker", false, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        ///     Controls if the lobby is enabled. If it is not, and there are no available jobs, you may get stuck on a black screen.
        /// </summary>
        public static readonly CVarDef<bool>
            GameLobbyEnabled = CVarDef.Create("game.lobbyenabled", false, CVar.ARCHIVE);

        /// <summary>
        ///     Controls the duration of the lobby timer in seconds. Defaults to 2 minutes and 30 seconds.
        /// </summary>
        public static readonly CVarDef<int>
            GameLobbyDuration = CVarDef.Create("game.lobbyduration", 150, CVar.ARCHIVE);

        /// <summary>
        ///     Controls if players can latejoin at all.
        /// </summary>
        public static readonly CVarDef<bool>
            GameDisallowLateJoins = CVarDef.Create("game.disallowlatejoins", false, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        ///     Controls the default game preset.
        /// </summary>
        public static readonly CVarDef<string>
            GameLobbyDefaultPreset = CVarDef.Create("game.defaultpreset", "secret", CVar.ARCHIVE);

        /// <summary>
        ///     Controls if the game can force a different preset if the current preset's criteria are not met.
        /// </summary>
        public static readonly CVarDef<bool>
            GameLobbyFallbackEnabled = CVarDef.Create("game.fallbackenabled", true, CVar.ARCHIVE);

        /// <summary>
        ///     The preset for the game to fall back to if the selected preset could not be used, and fallback is enabled.
        /// </summary>
        public static readonly CVarDef<string>
            GameLobbyFallbackPreset = CVarDef.Create("game.fallbackpreset", "sandbox", CVar.ARCHIVE);

        /// <summary>
        ///     Controls if people can win the game in Suspicion or Deathmatch.
        /// </summary>
        public static readonly CVarDef<bool>
            GameLobbyEnableWin = CVarDef.Create("game.enablewin", true, CVar.ARCHIVE);

        /// <summary>
        ///     Controls the maximum number of character slots a player is allowed to have.
        /// </summary>
        public static readonly CVarDef<int>
            GameMaxCharacterSlots = CVarDef.Create("game.maxcharacterslots", 10, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        ///     Controls the game map prototype to load. SS14 stores these prototypes in Prototypes/Maps.
        /// </summary>
        public static readonly CVarDef<string>
            GameMap = CVarDef.Create("game.map", "saltern", CVar.SERVERONLY);

        /// <summary>
        ///     Controls if the game should obey map criteria or not. Overriden if a map vote or similar occurs.
        /// </summary>
        public static readonly CVarDef<bool>
            GameMapForced = CVarDef.Create("game.mapforced", false, CVar.SERVERONLY);

        /// <summary>
        /// The depth of the queue used to calculate which map is next in rotation.
        /// This is how long the game "remembers" that some map was put in play. Default is 16 rounds.
        /// </summary>
        public static readonly CVarDef<int>
            GameMapMemoryDepth = CVarDef.Create("game.map_memory_depth", 16, CVar.SERVERONLY);

        /// <summary>
        /// Is map rotation enabled?
        /// </summary>
        public static readonly CVarDef<bool>
            GameMapRotation = CVarDef.Create<bool>("game.map_rotation", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether a random position offset will be applied to the station on roundstart.
        /// </summary>
        public static readonly CVarDef<bool> StationOffset =
            CVarDef.Create("game.station_offset", true);

        /// <summary>
        /// When the default blueprint is loaded what is the maximum amount it can be offset from 0,0.
        /// Does nothing without <see cref="StationOffset"/> as true.
        /// </summary>
        public static readonly CVarDef<float> MaxStationOffset =
            CVarDef.Create("game.maxstationoffset", 1000.0f);

        /// <summary>
        ///     Whether a random rotation will be applied to the station on roundstart.
        /// </summary>
        public static readonly CVarDef<bool> StationRotation =
            CVarDef.Create("game.station_rotation", true);

        /// <summary>
        ///     When enabled, guests will be assigned permanent UIDs and will have their preferences stored.
        /// </summary>
        public static readonly CVarDef<bool> GamePersistGuests =
            CVarDef.Create("game.persistguests", true, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool> GameDiagonalMovement =
            CVarDef.Create("game.diagonalmovement", true, CVar.ARCHIVE);

        public static readonly CVarDef<int> SoftMaxPlayers =
            CVarDef.Create("game.soft_max_players", 30, CVar.SERVERONLY | CVar.ARCHIVE);

#if EXCEPTION_TOLERANCE
        /// <summary>
        ///     Amount of times round start must fail before the server is shut down.
        ///     Set to 0 or a negative number to disable.
        /// </summary>
        public static readonly CVarDef<int> RoundStartFailShutdownCount =
            CVarDef.Create("game.round_start_fail_shutdown_count", 5, CVar.SERVERONLY | CVar.SERVER);
#endif

        /*
         * Discord
         */

        public static readonly CVarDef<string> DiscordAHelpWebhook =
            CVarDef.Create("discord.ahelp_webhook", string.Empty, CVar.SERVERONLY);

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
            CVarDef.Create("traitor.max_traitors", 7);

        public static readonly CVarDef<int> TraitorPlayersPerTraitor =
            CVarDef.Create("traitor.players_per_traitor", 5);

        public static readonly CVarDef<int> TraitorCodewordCount =
            CVarDef.Create("traitor.codeword_count", 4);

        public static readonly CVarDef<int> TraitorStartingBalance =
            CVarDef.Create("traitor.starting_balance", 20);

        public static readonly CVarDef<int> TraitorMaxDifficulty =
            CVarDef.Create("traitor.max_difficulty", 5);

        public static readonly CVarDef<int> TraitorMaxPicks =
            CVarDef.Create("traitor.max_picks", 20);

        /*
         * TraitorDeathMatch
         */

        public static readonly CVarDef<int> TraitorDeathMatchStartingBalance =
            CVarDef.Create("traitordm.starting_balance", 20);

        /*
         * Nukeops
         */

        public static readonly CVarDef<int> NukeopsMinPlayers =
            CVarDef.Create("nukeops.min_players", 15);

        public static readonly CVarDef<int> NukeopsMaxOps =
            CVarDef.Create("nukeops.max_ops", 6);

        public static readonly CVarDef<int> NukeopsPlayersPerOp =
            CVarDef.Create("nukeops.players_per_op", 5);

        /*
         * Pirates
         */

        public static readonly CVarDef<int> PiratesMinPlayers =
            CVarDef.Create("pirates.min_players", 25);

        public static readonly CVarDef<int> PiratesMaxOps =
            CVarDef.Create("pirates.max_pirates", 6);

        public static readonly CVarDef<int> PiratesPlayersPerOp =
            CVarDef.Create("pirates.players_per_pirate", 5);

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

        /// <summary>
        /// Milliseconds to asynchronously delay all SQLite database acquisitions with.
        /// </summary>
        /// <remarks>
        /// Defaults to 1 on DEBUG, 0 on RELEASE.
        /// This is intended to help catch .Result deadlock bugs that only happen on postgres
        /// (because SQLite is not actually asynchronous normally)
        /// </remarks>
        public static readonly CVarDef<int> DatabaseSqliteDelay =
            CVarDef.Create("database.sqlite_delay", DefaultSqliteDelay, CVar.SERVERONLY);

#if DEBUG
        private const int DefaultSqliteDelay = 1;
#else
        private const int DefaultSqliteDelay = 0;
#endif


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

        public static readonly CVarDef<bool> ParallaxLowQuality =
            CVarDef.Create("parallax.low_quality", false, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * Physics
         */

        /// <summary>
        /// When a mob is walking should its X / Y movement be relative to its parent (true) or the map (false).
        /// </summary>
        public static readonly CVarDef<bool> RelativeMovement =
            CVarDef.Create("physics.relative_movement", true, CVar.ARCHIVE | CVar.REPLICATED);

        public static readonly CVarDef<float> TileFrictionModifier =
            CVarDef.Create("physics.tile_friction", 40.0f);

        public static readonly CVarDef<float> StopSpeed =
            CVarDef.Create("physics.stop_speed", 0.1f);

        /// <summary>
        /// Whether mobs can push objects like lockers.
        /// </summary>
        /// <remarks>
        /// Technically client doesn't need to know about it but this may prevent a bug in the distant future so it stays.
        /// </remarks>
        public static readonly CVarDef<bool> MobPushing =
            CVarDef.Create("physics.mob_pushing", false, CVar.REPLICATED);

        /*
         * Lobby music
         */

        public static readonly CVarDef<bool> LobbyMusicEnabled =
            CVarDef.Create("ambience.lobbymusicenabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * Admin sounds
         */

        public static readonly CVarDef<bool> AdminSoundsEnabled =
            CVarDef.Create("audio.adminsoundsenabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * HUD
         */

        public static readonly CVarDef<int> HudTheme =
            CVarDef.Create("hud.theme", 0, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<bool> HudHeldItemShow =
            CVarDef.Create("hud.held_item_show", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<float> HudHeldItemOffset =
            CVarDef.Create("hud.held_item_offset", 28f, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<bool> HudFpsCounterVisible =
            CVarDef.Create("hud.fps_counter_visible", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * NPCs
         */

        public static readonly CVarDef<int> NPCMaxUpdates =
            CVarDef.Create("npc.max_updates", 64);

        public static readonly CVarDef<bool> NPCEnabled = CVarDef.Create("npc.enabled", true);

        /*
         * Net
         */

        public static readonly CVarDef<float> NetAtmosDebugOverlayTickRate =
            CVarDef.Create("net.atmosdbgoverlaytickrate", 3.0f);

        public static readonly CVarDef<float> NetGasOverlayTickRate =
            CVarDef.Create("net.gasoverlaytickrate", 3.0f);

        public static readonly CVarDef<int> GasOverlayThresholds =
            CVarDef.Create("net.gasoverlaythresholds", 20);

        /*
         * Admin stuff
         */

        public static readonly CVarDef<bool> AdminAnnounceLogin =
            CVarDef.Create("admin.announce_login", true, CVar.SERVERONLY);

        public static readonly CVarDef<bool> AdminAnnounceLogout =
            CVarDef.Create("admin.announce_logout", true, CVar.SERVERONLY);

        /*
         * Explosions
         */

        /// <summary>
        ///     How many tiles the explosion system will process per tick
        /// </summary>
        /// <remarks>
        ///     Setting this too high will put a large load on a single tick. Setting this too low will lead to
        ///     unnaturally "slow" explosions.
        /// </remarks>
        public static readonly CVarDef<int> ExplosionTilesPerTick =
            CVarDef.Create("explosion.tiles_per_tick", 100, CVar.SERVERONLY);

        /// <summary>
        ///     Upper limit on the size of an explosion before physics-throwing is disabled.
        /// </summary>
        /// <remarks>
        ///     Large nukes tend to generate a lot of shrapnel that flies through space. This can functionally cripple
        ///     the server TPS for a while after an explosion (or even during, if the explosion is processed
        ///     incrementally.
        /// </remarks>
        public static readonly CVarDef<int> ExplosionThrowLimit =
            CVarDef.Create("explosion.throw_limit", 400, CVar.SERVERONLY);

        /// <summary>
        ///     If this is true, explosion processing will pause the NodeGroupSystem to pause updating.
        /// </summary>
        /// <remarks>
        ///     This only takes effect if an explosion needs more than one tick to process (i.e., covers more than <see
        ///     cref="ExplosionTilesPerTick"/> tiles). If this is not enabled, the node-system will rebuild its graph
        ///     every tick as the explosion shreds the station, causing significant slowdown.
        /// </remarks>
        public static readonly CVarDef<bool> ExplosionSleepNodeSys =
            CVarDef.Create("explosion.node_sleep", true, CVar.SERVERONLY);

        /// <summary>
        ///     Upper limit on the total area that an explosion can affect before the neighbor-finding algorithm just
        ///     stops. Defaults to a 60-rile radius explosion.
        /// </summary>
        /// <remarks>
        ///     Actual area may be larger, as it currently doesn't terminate mid neighbor finding. I.e., area may be that of a ~51 tile radius circle instead.
        /// </remarks>
        public static readonly CVarDef<int> ExplosionMaxArea =
            CVarDef.Create("explosion.max_area", (int) 3.14f * 50 * 50, CVar.SERVERONLY);

        /// <summary>
        ///     Upper limit on the number of neighbor finding steps for the explosion system neighbor-finding algorithm.
        /// </summary>
        /// <remarks>
        ///     Effectively places an upper limit on the range that any explosion can have. In the vast majority of
        ///     instances, <see cref="ExplosionMaxArea"/> will likely be hit before this becomes a limiting factor.
        /// </remarks>
        public static readonly CVarDef<int> ExplosionMaxIterations =
            CVarDef.Create("explosion.max_iterations", 150, CVar.SERVERONLY);

        /// <summary>
        ///     Max Time in milliseconds to spend processing explosions every tick.
        /// </summary>
        /// <remarks>
        ///     This time limiting is not perfectly implemented. Firstly, a significant chunk of processing time happens
        ///     due to queued entity deletions, which happen outside of the system update code. Secondly, explosion
        ///     spawning cannot currently be interrupted & resumed, and may lead to exceeding this time limit.
        /// </remarks>
        public static readonly CVarDef<float> ExplosionMaxProcessingTime =
            CVarDef.Create("explosion.max_tick_time", 7f, CVar.SERVERONLY);

        /// <summary>
        ///     If the explosion is being processed incrementally over several ticks, this variable determines whether
        ///     updating the grid tiles should be done incrementally at the end of every tick, or only once the explosion has finished processing.
        /// </summary>
        /// <remarks>
        ///     The most notable consequence of this change is that explosions will only punch a hole in the station &
        ///     create a vacumm once they have finished exploding. So airlocks will no longer slam shut as the explosion
        ///     expands, just suddenly at the end.
        /// </remarks>
        public static readonly CVarDef<bool> ExplosionIncrementalTileBreaking =
            CVarDef.Create("explosion.incremental_tile", false, CVar.SERVERONLY);

        /// <summary>
        ///     Client-side explosion visuals: for how many seconds should an explosion stay on-screen once it has
        ///     finished expanding?
        /// </summary>
        public static readonly CVarDef<float> ExplosionPersistence =
            CVarDef.Create("explosion.persistence", 0.3f, CVar.REPLICATED);

        /// <summary>
        ///     If an explosion covers a larger area than this number, the damaging/processing will always start during
        ///     the next tick, instead of during the same tick that the explosion was generated in.
        /// </summary>
        /// <remarks>
        ///     This value can be used to ensure that for large explosions the area/tile calculation and the explosion
        ///     processing/damaging occurs in separate ticks. This helps reduce the single-tick lag if both <see
        ///     cref="ExplosionMaxProcessingTime"/> and <see cref="ExplosionTilesPerTick"/> are large. I.e., instead of
        ///     a single tick explosion, this cvar allows for a configuration that results in a two-tick explosion,
        ///     though most of the computational cost is still in the second tick.
        /// </remarks>
        public static readonly CVarDef<int> ExplosionSingleTickAreaLimit =
            CVarDef.Create("explosion.single_tick_area_limit", 400, CVar.SERVERONLY);

        /*
         * Admin logs
         */

        /// <summary>
        ///     Controls if admin logs are enabled. Highly recommended to shut this off for development.
        /// </summary>
        public static readonly CVarDef<bool> AdminLogsEnabled =
            CVarDef.Create("adminlogs.enabled", true, CVar.SERVERONLY);

        public static readonly CVarDef<float> AdminLogsQueueSendDelay =
            CVarDef.Create("adminlogs.queue_send_delay_seconds", 5f, CVar.SERVERONLY);

        public static readonly CVarDef<int> AdminLogsQueueMax =
            CVarDef.Create("adminlogs.queue_max", 5000, CVar.SERVERONLY);

        public static readonly CVarDef<int> AdminLogsPreRoundQueueMax =
            CVarDef.Create("adminlogs.pre_round_queue_max", 5000, CVar.SERVERONLY);

        // How many logs to send to the client at once
        public static readonly CVarDef<int> AdminLogsClientBatchSize =
            CVarDef.Create("adminlogs.client_batch_size", 1000, CVar.SERVERONLY);

        public static readonly CVarDef<string> AdminLogsServerName =
            CVarDef.Create("adminlogs.server_name", "unknown", CVar.SERVERONLY);

        /*
         * Atmos
         */

        /// <summary>
        ///     Whether gas differences will move entities.
        /// </summary>
        public static readonly CVarDef<bool> SpaceWind =
            CVarDef.Create("atmos.space_wind", false, CVar.SERVERONLY);

        /// <summary>
        ///     Divisor from maxForce (pressureDifference * 2.25f) to force applied on objects.
        /// </summary>
        public static readonly CVarDef<float> SpaceWindPressureForceDivisorThrow =
            CVarDef.Create("atmos.space_wind_pressure_force_divisor_throw", 15f, CVar.SERVERONLY);

        /// <summary>
        ///     Divisor from maxForce (pressureDifference * 2.25f) to force applied on objects.
        /// </summary>
        public static readonly CVarDef<float> SpaceWindPressureForceDivisorPush =
            CVarDef.Create("atmos.space_wind_pressure_force_divisor_push", 2500f, CVar.SERVERONLY);

        /// <summary>
        ///     The maximum velocity (not force) that may be applied to an object by atmospheric pressure differences.
        ///     Useful to prevent clipping through objects.
        /// </summary>
        public static readonly CVarDef<float> SpaceWindMaxVelocity =
            CVarDef.Create("atmos.space_wind_max_velocity", 30f, CVar.SERVERONLY);

        /// <summary>
        ///     The maximum force that may be applied to an object by pushing (i.e. not throwing) atmospheric pressure differences.
        ///     A "throwing" atmospheric pressure difference ignores this limit, but not the max. velocity limit.
        /// </summary>
        public static readonly CVarDef<float> SpaceWindMaxPushForce =
            CVarDef.Create("atmos.space_wind_max_push_force", 20f, CVar.SERVERONLY);

        /// <summary>
        ///     Whether monstermos tile equalization is enabled.
        /// </summary>
        public static readonly CVarDef<bool> MonstermosEqualization =
            CVarDef.Create("atmos.monstermos_equalization", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether monstermos explosive depressurization is enabled.
        ///     Needs <see cref="MonstermosEqualization"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> MonstermosDepressurization =
            CVarDef.Create<bool>("atmos.monstermos_depressurization", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether monstermos explosive depressurization will rip tiles..
        ///     Needs <see cref="MonstermosEqualization"/> and <see cref="MonstermosDepressurization"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> MonstermosRipTiles =
            CVarDef.Create<bool>("atmos.monstermos_rip_tiles", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether explosive depressurization will cause the grid to gain an impulse.
        ///     Needs <see cref="MonstermosEqualization"/> and <see cref="MonstermosDepressurization"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> AtmosGridImpulse =
            CVarDef.Create("atmos.grid_impulse", false, CVar.SERVERONLY);

        /// <summary>
        ///     Whether atmos superconduction is enabled.
        /// </summary>
        /// <remarks> Disabled by default, superconduction is awful. </remarks>
        public static readonly CVarDef<bool> Superconduction =
            CVarDef.Create("atmos.superconduction", false, CVar.SERVERONLY);

        /// <summary>
        ///     Whether excited groups will be processed and created.
        /// </summary>
        public static readonly CVarDef<bool> ExcitedGroups =
            CVarDef.Create("atmos.excited_groups", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether all tiles in an excited group will clear themselves once being exposed to space.
        ///     Similar to <see cref="MonstermosDepressurization"/>, without none of the tile ripping or
        ///     things being thrown around very violently.
        ///     Needs <see cref="ExcitedGroups"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> ExcitedGroupsSpaceIsAllConsuming =
            CVarDef.Create("atmos.excited_groups_space_is_all_consuming", false, CVar.SERVERONLY);

        /// <summary>
        ///     Maximum time in milliseconds that atmos can take processing.
        /// </summary>
        public static readonly CVarDef<float> AtmosMaxProcessTime =
            CVarDef.Create("atmos.max_process_time", 3f, CVar.SERVERONLY);

        /// <summary>
        ///     Atmos tickrate in TPS. Atmos processing will happen every 1/TPS seconds.
        /// </summary>
        public static readonly CVarDef<float> AtmosTickRate =
            CVarDef.Create("atmos.tickrate", 15f, CVar.SERVERONLY);

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

        public static readonly CVarDef<bool> OocEnabled = CVarDef.Create("ooc.enabled", true, CVar.NOTIFY | CVar.REPLICATED);

        public static readonly CVarDef<bool> AdminOocEnabled =
            CVarDef.Create("ooc.enabled_admin", true, CVar.NOTIFY);

        /// <summary>
        /// If true, whenever OOC is disabled the Discord OOC relay will also be disabled.
        /// </summary>
        public static readonly CVarDef<bool> DisablingOOCDisablesRelay = CVarDef.Create("ooc.disabling_ooc_disables_relay", true, CVar.SERVERONLY);

        /*
         * LOOC
         */

        public static readonly CVarDef<bool> LoocEnabled = CVarDef.Create("looc.enabled", true, CVar.NOTIFY | CVar.REPLICATED);

        public static readonly CVarDef<bool> AdminLoocEnabled =
            CVarDef.Create("looc.enabled_admin", true, CVar.NOTIFY);

        /*
         * Entity Menu Grouping Types
         */
        public static readonly CVarDef<int> EntityMenuGroupingType = CVarDef.Create("entity_menu", 0, CVar.CLIENTONLY);

        /*
         * Whitelist
         */

        /// <summary>
        ///     Controls whether the server will deny any players that are not whitelisted in the DB.
        /// </summary>
        public static readonly CVarDef<bool> WhitelistEnabled =
            CVarDef.Create("whitelist.enabled", false, CVar.SERVERONLY);

        /*
         * VOTE
         */

        /// <summary>
        ///     Allows enabling/disabling player-started votes for ultimate authority
        /// </summary>
        public static readonly CVarDef<bool> VoteEnabled =
            CVarDef.Create("vote.enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     See vote.enabled, but specific to restart votes
        /// </summary>
        public static readonly CVarDef<bool> VoteRestartEnabled =
            CVarDef.Create("vote.restart_enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     See vote.enabled, but specific to preset votes
        /// </summary>
        public static readonly CVarDef<bool> VotePresetEnabled =
            CVarDef.Create("vote.preset_enabled", true, CVar.SERVERONLY);

        /// <summary>
        ///     See vote.enabled, but specific to map votes
        /// </summary>
        public static readonly CVarDef<bool> VoteMapEnabled =
            CVarDef.Create("vote.map_enabled", false, CVar.SERVERONLY);

        /// <summary>
        ///     The required ratio of the server that must agree for a restart round vote to go through.
        /// </summary>
        public static readonly CVarDef<float> VoteRestartRequiredRatio =
            CVarDef.Create("vote.restart_required_ratio", 0.7f, CVar.SERVERONLY);

        /// <summary>
        ///     The delay which two votes of the same type are allowed to be made by separate people, in seconds.
        /// </summary>
        public static readonly CVarDef<float> VoteSameTypeTimeout =
            CVarDef.Create("vote.same_type_timeout", 240f, CVar.SERVERONLY);


        /// <summary>
        ///     Sets the duration of the map vote timer.
        /// </summary>
        public static readonly CVarDef<int>
            VoteTimerMap = CVarDef.Create("vote.timermap", 90, CVar.SERVERONLY);

        /// <summary>
        ///     Sets the duration of the restart vote timer.
        /// </summary>
        public static readonly CVarDef<int>
            VoteTimerRestart = CVarDef.Create("vote.timerrestart", 60, CVar.SERVERONLY);

        /// <summary>
        ///     Sets the duration of the gamemode/preset vote timer.
        /// </summary>
        public static readonly CVarDef<int>
            VoteTimerPreset = CVarDef.Create("vote.timerpreset", 30, CVar.SERVERONLY);

        /// <summary>
        ///     Sets the duration of the map vote timer when ALONE.
        /// </summary>
        public static readonly CVarDef<int>
            VoteTimerAlone = CVarDef.Create("vote.timeralone", 10, CVar.SERVERONLY);


        /*
         * BAN
         */

        public static readonly CVarDef<bool> BanHardwareIds =
            CVarDef.Create("ban.hardware_ids", true, CVar.SERVERONLY);

        /*
         * Shuttles
         */
        public static readonly CVarDef<float> ShuttleMaxLinearSpeed =
            CVarDef.Create("shuttle.max_linear_speed", 13f, CVar.SERVERONLY);

        public static readonly CVarDef<float> ShuttleMaxAngularSpeed =
            CVarDef.Create("shuttle.max_angular_speed", 1.4f, CVar.SERVERONLY);

        public static readonly CVarDef<float> ShuttleMaxAngularAcc =
            CVarDef.Create("shuttle.max_angular_acc", 2f, CVar.SERVERONLY);

        public static readonly CVarDef<float> ShuttleMaxAngularMomentum =
            CVarDef.Create("shuttle.max_angular_momentum", 60000f, CVar.SERVERONLY);

        public static readonly CVarDef<float> ShuttleIdleLinearDamping =
            CVarDef.Create("shuttle.idle_linear_damping", 50f, CVar.SERVERONLY);

        public static readonly CVarDef<float> ShuttleIdleAngularDamping =
            CVarDef.Create("shuttle.idle_angular_damping", 100f, CVar.SERVERONLY);


        /*
         * VIEWPORT
         */

        public static readonly CVarDef<bool> ViewportStretch =
            CVarDef.Create("viewport.stretch", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> ViewportFixedScaleFactor =
            CVarDef.Create("viewport.fixed_scale_factor", 2, CVar.CLIENTONLY | CVar.ARCHIVE);

        // This default is basically specifically chosen so fullscreen/maximized 1080p hits a 2x snap and does NN.
        public static readonly CVarDef<int> ViewportSnapToleranceMargin =
            CVarDef.Create("viewport.snap_tolerance_margin", 64, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> ViewportSnapToleranceClip =
            CVarDef.Create("viewport.snap_tolerance_clip", 32, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<bool> ViewportScaleRender =
            CVarDef.Create("viewport.scale_render", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * CHAT
         */

        public static readonly CVarDef<int> ChatMaxMessageLength =
            CVarDef.Create("chat.max_message_length", 1000, CVar.SERVER | CVar.REPLICATED);

        public static readonly CVarDef<bool> ChatSanitizerEnabled =
            CVarDef.Create("chat.chat_sanitizer_enabled", true, CVar.SERVERONLY);

        public static readonly CVarDef<bool> ChatShowTypingIndicator =
            CVarDef.Create("chat.show_typing_indicator", true, CVar.CLIENTONLY);

        /*
         * AFK
         */

        /// <summary>
        /// How long a client can go without any input before being considered AFK.
        /// </summary>
        public static readonly CVarDef<float> AfkTime =
            CVarDef.Create("afk.time", 60f, CVar.SERVERONLY);

        /*
         * IC
         */

        /// <summary>
        /// Restricts IC character names to alphanumeric chars.
        /// </summary>
        public static readonly CVarDef<bool> RestrictedNames =
            CVarDef.Create("ic.restricted_names", true, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Allows flavor text (character descriptions)
        /// </summary>
        public static readonly CVarDef<bool> FlavorText =
            CVarDef.Create("ic.flavor_text", false, CVar.SERVER | CVar.REPLICATED);

        /*
         * Salvage
         */

        /// <summary>
        ///     Forced salvage map prototype name (if empty, randomly selected)
        /// </summary>
        public static readonly CVarDef<string>
            SalvageForced = CVarDef.Create("salvage.forced", "", CVar.SERVERONLY);

        /*
         * Rules
         */

        /// <summary>
        /// Time that players have to wait before rules can be accepted.
        /// </summary>
        public static readonly CVarDef<float> RulesWaitTime =
            CVarDef.Create("rules.time", 45f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Don't show rules to localhost/loopback interface.
        /// </summary>
        public static readonly CVarDef<bool> RulesExemptLocal =
            CVarDef.Create("rules.exempt_local", true, CVar.SERVERONLY);


        /*
         * Autogeneration
         */

        public static readonly CVarDef<string> DestinationFile =
            CVarDef.Create("autogen.destination_file", "", CVar.SERVER | CVar.SERVERONLY);

        /*
         * Network Resource Manager
         */

        /// <summary>
        /// Controls whether new resources can be uploaded by admins.
        /// Does not prevent already uploaded resources from being sent.
        /// </summary>
        public static readonly CVarDef<bool> ResourceUploadingEnabled =
            CVarDef.Create("netres.enabled", true, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Controls the data size limit in megabytes for uploaded resources. If they're too big, they will be dropped.
        /// Set to zero or a negative value to disable limit.
        /// </summary>
        public static readonly CVarDef<float> ResourceUploadingLimitMb =
            CVarDef.Create("netres.limit", 3f, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Whether uploaded files will be stored in the server's database.
        /// This is useful to keep "logs" on what files admins have uploaded in the past.
        /// </summary>
        public static readonly CVarDef<bool> ResourceUploadingStoreEnabled =
            CVarDef.Create("netres.store_enabled", true, CVar.SERVER | CVar.SERVERONLY);

        /// <summary>
        /// Numbers of days before stored uploaded files are deleted. Set to zero or negative to disable auto-delete.
        /// This is useful to free some space automatically. Auto-deletion runs only on server boot.
        /// </summary>
        public static readonly CVarDef<int> ResourceUploadingStoreDeletionDays =
            CVarDef.Create("netres.store_deletion_days", 30, CVar.SERVER | CVar.SERVERONLY);

        /*
         * Controls
         */

        /// <summary>
        /// Deadzone for drag-drop interactions.
        /// </summary>
        public static readonly CVarDef<float> DragDropDeadZone =
            CVarDef.Create("control.drag_dead_zone", 12f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * UPDATE
         */

        /// <summary>
        /// If a server update restart is pending, the delay after the last player leaves before we actually restart. In seconds.
        /// </summary>
        public static readonly CVarDef<float> UpdateRestartDelay =
            CVarDef.Create("update.restart_delay", 20f, CVar.SERVERONLY);

        /*
         * Ghost
         */

        /// <summary>
        /// The time you must spend reading the rules, before the "Request" button is enabled
        /// </summary>
        public static readonly CVarDef<float> GhostRoleTime =
            CVarDef.Create("ghost.role_time", 3f, CVar.REPLICATED);
    }
}
