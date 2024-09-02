using Content.Shared.Maps;
using Content.Shared.Roles;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;

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
        ///     Guide Entry Prototype ID to be displayed as the server rules.
        /// </summary>
        public static readonly CVarDef<string> RulesFile =
            CVarDef.Create("server.rules_file", "DefaultRuleset", CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        ///     Guide entry that is displayed by default when a guide is opened.
        /// </summary>
        public static readonly CVarDef<string> DefaultGuide =
            CVarDef.Create("server.default_guide", "NewPlayer", CVar.REPLICATED | CVar.SERVER);

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
            CVarDef.Create("ambience.range", 8f, CVar.REPLICATED | CVar.SERVER);

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
            CVarDef.Create("ambience.volume", 1.5f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// Ambience music volume.
        /// </summary>
        public static readonly CVarDef<float> AmbientMusicVolume =
            CVarDef.Create("ambience.music_volume", 1.5f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// Lobby / round end music volume.
        /// </summary>
        public static readonly CVarDef<float> LobbyMusicVolume =
            CVarDef.Create("ambience.lobby_music_volume", 0.50f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// UI volume.
        /// </summary>
        public static readonly CVarDef<float> InterfaceVolume =
            CVarDef.Create("audio.interface_volume", 0.50f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * Status
         */

        public static readonly CVarDef<string> StatusMoMMIUrl =
            CVarDef.Create("status.mommiurl", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> StatusMoMMIPassword =
            CVarDef.Create("status.mommipassword", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

        /*
         * Events
         */

        /// <summary>
        ///     Controls if the game should run station events
        /// </summary>
        public static readonly CVarDef<bool>
            EventsEnabled = CVarDef.Create("events.enabled", true, CVar.ARCHIVE | CVar.SERVERONLY);

        /*
         * Game
         */

        /// <summary>
        ///     Disables most functionality in the GameTicker.
        /// </summary>
        public static readonly CVarDef<bool>
            GameDummyTicker = CVarDef.Create("game.dummyticker", false, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        ///     Controls if the lobby is enabled. If it is not, and there are no available jobs, you may get stuck on a black screen.
        /// </summary>
        public static readonly CVarDef<bool>
            GameLobbyEnabled = CVarDef.Create("game.lobbyenabled", true, CVar.ARCHIVE);

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
            GameLobbyFallbackPreset = CVarDef.Create("game.fallbackpreset", "Traitor,Extended", CVar.ARCHIVE);

        /// <summary>
        ///     Controls if people can win the game in Suspicion or Deathmatch.
        /// </summary>
        public static readonly CVarDef<bool>
            GameLobbyEnableWin = CVarDef.Create("game.enablewin", true, CVar.ARCHIVE);

        /// <summary>
        ///     Controls the maximum number of character slots a player is allowed to have.
        /// </summary>
        public static readonly CVarDef<int>
            GameMaxCharacterSlots = CVarDef.Create("game.maxcharacterslots", 30, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        ///     Controls the game map prototype to load. SS14 stores these prototypes in Prototypes/Maps.
        /// </summary>
        public static readonly CVarDef<string>
            GameMap = CVarDef.Create("game.map", string.Empty, CVar.SERVERONLY);

        /// <summary>
        ///     Controls whether to use world persistence or not.
        /// </summary>
        public static readonly CVarDef<bool>
            UsePersistence = CVarDef.Create("game.usepersistence", false, CVar.ARCHIVE);

        /// <summary>
        ///     If world persistence is used, what map prototype should be initially loaded.
        ///     If the save file exists, it replaces MapPath but everything else stays the same (station name and such).
        /// </summary>
        public static readonly CVarDef<string>
            PersistenceMap = CVarDef.Create("game.persistencemap", "Empty", CVar.ARCHIVE);

        /// <summary>
        ///     Prototype to use for map pool.
        /// </summary>
        public static readonly CVarDef<string>
            GameMapPool = CVarDef.Create("game.map_pool", "DefaultMapPool", CVar.SERVERONLY);

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
            GameMapRotation = CVarDef.Create("game.map_rotation", true, CVar.SERVERONLY);

        /// <summary>
        /// If roles should be restricted based on time.
        /// </summary>
        public static readonly CVarDef<bool>
            GameRoleTimers = CVarDef.Create("game.role_timers", true, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Override default role requirements using a <see cref="JobRequirementOverridePrototype"/>
        /// </summary>
        public static readonly CVarDef<string>
            GameRoleTimerOverride = CVarDef.Create("game.role_timer_override", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// If roles should be restricted based on whether or not they are whitelisted.
        /// </summary>
        public static readonly CVarDef<bool>
            GameRoleWhitelist = CVarDef.Create("game.role_whitelist", true, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Whether or not disconnecting inside of a cryopod should remove the character or just store them until they reconnect.
        /// </summary>
        public static readonly CVarDef<bool>
            GameCryoSleepRejoining = CVarDef.Create("game.cryo_sleep_rejoining", false, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     When enabled, guests will be assigned permanent UIDs and will have their preferences stored.
        /// </summary>
        public static readonly CVarDef<bool> GamePersistGuests =
            CVarDef.Create("game.persistguests", true, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool> GameDiagonalMovement =
            CVarDef.Create("game.diagonalmovement", true, CVar.ARCHIVE);

        public static readonly CVarDef<int> SoftMaxPlayers =
            CVarDef.Create("game.soft_max_players", 30, CVar.SERVERONLY | CVar.ARCHIVE);

        /// <summary>
        /// If a player gets denied connection to the server,
        /// how long they are forced to wait before attempting to reconnect.
        /// </summary>
        public static readonly CVarDef<int> GameServerFullReconnectDelay =
            CVarDef.Create("game.server_full_reconnect_delay", 30, CVar.SERVERONLY);

        /// <summary>
        /// Whether or not panic bunker is currently enabled.
        /// </summary>
        public static readonly CVarDef<bool> PanicBunkerEnabled =
            CVarDef.Create("game.panic_bunker.enabled", false, CVar.NOTIFY | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Whether or not the panic bunker will disable when an admin comes online.
        /// </summary>
        public static readonly CVarDef<bool> PanicBunkerDisableWithAdmins =
            CVarDef.Create("game.panic_bunker.disable_with_admins", false, CVar.SERVERONLY);

        /// <summary>
        /// Whether or not the panic bunker will enable when no admins are online.
        /// </summary>
        public static readonly CVarDef<bool> PanicBunkerEnableWithoutAdmins =
            CVarDef.Create("game.panic_bunker.enable_without_admins", false, CVar.SERVERONLY);

        /// <summary>
        /// Whether or not the panic bunker will count deadminned admins for
        /// <see cref="PanicBunkerDisableWithAdmins"/> and
        /// <see cref="PanicBunkerEnableWithoutAdmins"/>
        /// </summary>
        public static readonly CVarDef<bool> PanicBunkerCountDeadminnedAdmins =
            CVarDef.Create("game.panic_bunker.count_deadminned_admins", false, CVar.SERVERONLY);

        /// <summary>
        /// Show reason of disconnect for user or not.
        /// </summary>
        public static readonly CVarDef<bool> PanicBunkerShowReason =
            CVarDef.Create("game.panic_bunker.show_reason", false, CVar.SERVERONLY);

        /// <summary>
        /// Minimum age of the account (from server's PoV, so from first-seen date) in minutes.
        /// </summary>
        public static readonly CVarDef<int> PanicBunkerMinAccountAge =
            CVarDef.Create("game.panic_bunker.min_account_age", 1440, CVar.SERVERONLY);

        /// <summary>
        /// Minimal overall played time.
        /// </summary>
        public static readonly CVarDef<int> PanicBunkerMinOverallMinutes =
            CVarDef.Create("game.panic_bunker.min_overall_minutes", 600, CVar.SERVERONLY);

        /// <summary>
        /// A custom message that will be used for connections denied to the panic bunker
        /// If not empty, then will overwrite <see cref="PanicBunkerShowReason"/>
        /// </summary>
        public static readonly CVarDef<string> PanicBunkerCustomReason =
            CVarDef.Create("game.panic_bunker.custom_reason", string.Empty, CVar.SERVERONLY);

        /// <summary>
        /// Allow bypassing the panic bunker if the user is whitelisted.
        /// </summary>
        public static readonly CVarDef<bool> BypassBunkerWhitelist =
            CVarDef.Create("game.panic_bunker.whitelisted_can_bypass", true, CVar.SERVERONLY);

        /*
         * TODO: Remove baby jail code once a more mature gateway process is established. This code is only being issued as a stopgap to help with potential tiding in the immediate future.
         */

        /// <summary>
        /// Whether the baby jail is currently enabled.
        /// </summary>
        public static readonly CVarDef<bool> BabyJailEnabled  =
            CVarDef.Create("game.baby_jail.enabled", false, CVar.NOTIFY | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Show reason of disconnect for user or not.
        /// </summary>
        public static readonly CVarDef<bool> BabyJailShowReason =
            CVarDef.Create("game.baby_jail.show_reason", false, CVar.SERVERONLY);

        /// <summary>
        /// Maximum age of the account (from server's PoV, so from first-seen date) in minutes that can access baby
        /// jailed servers.
        /// </summary>
        public static readonly CVarDef<int> BabyJailMaxAccountAge =
            CVarDef.Create("game.baby_jail.max_account_age", 1440, CVar.SERVERONLY);

        /// <summary>
        /// Maximum overall played time allowed to access baby jailed servers.
        /// </summary>
        public static readonly CVarDef<int> BabyJailMaxOverallMinutes =
            CVarDef.Create("game.baby_jail.max_overall_minutes", 120, CVar.SERVERONLY);

        /// <summary>
        /// A custom message that will be used for connections denied due to the baby jail.
        /// If not empty, then will overwrite <see cref="BabyJailShowReason"/>
        /// </summary>
        public static readonly CVarDef<string> BabyJailCustomReason =
            CVarDef.Create("game.baby_jail.custom_reason", string.Empty, CVar.SERVERONLY);

        /// <summary>
        /// Allow bypassing the baby jail if the user is whitelisted.
        /// </summary>
        public static readonly CVarDef<bool> BypassBabyJailWhitelist =
            CVarDef.Create("game.baby_jail.whitelisted_can_bypass", true, CVar.SERVERONLY);

        /// <summary>
        /// Make people bonk when trying to climb certain objects like tables.
        /// </summary>
        public static readonly CVarDef<bool> GameTableBonk =
            CVarDef.Create("game.table_bonk", false, CVar.REPLICATED);

        /// <summary>
        /// Whether or not status icons are rendered for everyone.
        /// </summary>
        public static readonly CVarDef<bool> GlobalStatusIconsEnabled =
            CVarDef.Create("game.global_status_icons_enabled", true, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Whether or not status icons are rendered on this specific client.
        /// </summary>
        public static readonly CVarDef<bool> LocalStatusIconsEnabled =
            CVarDef.Create("game.local_status_icons_enabled", true, CVar.CLIENTONLY);

        /// <summary>
        /// Whether or not coordinates on the Debug overlay should only be available to admins.
        /// </summary>
        public static readonly CVarDef<bool> DebugCoordinatesAdminOnly =
            CVarDef.Create("game.debug_coordinates_admin_only", true, CVar.SERVER | CVar.REPLICATED);

#if EXCEPTION_TOLERANCE
        /// <summary>
        ///     Amount of times round start must fail before the server is shut down.
        ///     Set to 0 or a negative number to disable.
        /// </summary>
        public static readonly CVarDef<int> RoundStartFailShutdownCount =
            CVarDef.Create("game.round_start_fail_shutdown_count", 5, CVar.SERVERONLY | CVar.SERVER);
#endif

        /// <summary>
        /// Delay between station alert level changes.
        /// </summary>
        public static readonly CVarDef<int> GameAlertLevelChangeDelay =
            CVarDef.Create("game.alert_level_change_delay", 30, CVar.SERVERONLY);

        /// <summary>
        /// The time in seconds that the server should wait before restarting the round.
        /// Defaults to 2 minutes.
        /// </summary>
        public static readonly CVarDef<float> RoundRestartTime =
            CVarDef.Create("game.round_restart_time", 120f, CVar.SERVERONLY);

        /// <summary>
        /// The prototype to use for secret weights.
        /// </summary>
        public static readonly CVarDef<string> SecretWeightPrototype =
            CVarDef.Create("game.secret_weight_prototype", "Secret", CVar.SERVERONLY);

        /// <summary>
        /// The id of the sound collection to randomly choose a sound from and play when the round ends.
        /// </summary>
        public static readonly CVarDef<string> RoundEndSoundCollection =
            CVarDef.Create("game.round_end_sound_collection", "RoundEnd", CVar.SERVERONLY);

        /// <summary>
        /// Whether or not to add every player as a global override to PVS at round end.
        /// This will allow all players to see their clothing in the round screen player list screen,
        /// but may cause lag during round end with very high player counts.
        /// </summary>
        public static readonly CVarDef<bool> RoundEndPVSOverrides =
            CVarDef.Create("game.round_end_pvs_overrides", true, CVar.SERVERONLY);

        /// <summary>
        /// If true, players can place objects onto tabletop games like chess boards.
        /// </summary>
        /// <remarks>
        /// This feature is currently highly abusable and can easily be used to crash the server,
        /// so it's off by default.
        /// </remarks>
        public static readonly CVarDef<bool> GameTabletopPlace =
            CVarDef.Create("game.tabletop_place", false, CVar.SERVERONLY);

        /// <summary>
        /// If true, contraband severity can be viewed in the examine menu
        /// </summary>
        public static readonly CVarDef<bool> ContrabandExamine =
            CVarDef.Create("game.contraband_examine", true, CVar.SERVER | CVar.REPLICATED);

        /*
         * Discord
         */

        /// <summary>
        /// URL of the Discord webhook which will relay all ahelp messages.
        /// </summary>
        public static readonly CVarDef<string> DiscordAHelpWebhook =
            CVarDef.Create("discord.ahelp_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

        /// <summary>
        /// The server icon to use in the Discord ahelp embed footer.
        /// Valid values are specified at https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure.
        /// </summary>
        public static readonly CVarDef<string> DiscordAHelpFooterIcon =
            CVarDef.Create("discord.ahelp_footer_icon", string.Empty, CVar.SERVERONLY);

        /// <summary>
        /// The avatar to use for the webhook. Should be an URL.
        /// </summary>
        public static readonly CVarDef<string> DiscordAHelpAvatar =
            CVarDef.Create("discord.ahelp_avatar", string.Empty, CVar.SERVERONLY);

        /// <summary>
        /// URL of the Discord webhook which will relay all custom votes. If left empty, disables the webhook.
        /// </summary>
        public static readonly CVarDef<string> DiscordVoteWebhook =
            CVarDef.Create("discord.vote_webhook", string.Empty, CVar.SERVERONLY);

        /// URL of the Discord webhook which will relay round restart messages.
        /// </summary>
        public static readonly CVarDef<string> DiscordRoundUpdateWebhook =
            CVarDef.Create("discord.round_update_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

        /// <summary>
        /// Role id for the Discord webhook to ping when the round ends.
        /// </summary>
        public static readonly CVarDef<string> DiscordRoundEndRoleWebhook =
            CVarDef.Create("discord.round_end_role", string.Empty, CVar.SERVERONLY);

        /*
         * Tips
         */

        /// <summary>
        ///     Whether tips being shown is enabled at all.
        /// </summary>
        public static readonly CVarDef<bool> TipsEnabled =
            CVarDef.Create("tips.enabled", true);

        /// <summary>
        ///     The dataset prototype to use when selecting a random tip.
        /// </summary>
        public static readonly CVarDef<string> TipsDataset =
            CVarDef.Create("tips.dataset", "Tips");

        /// <summary>
        ///     The number of seconds between each tip being displayed when the round is not actively going
        ///     (i.e. postround or lobby)
        /// </summary>
        public static readonly CVarDef<float> TipFrequencyOutOfRound =
            CVarDef.Create("tips.out_of_game_frequency", 60f * 1.5f);

        /// <summary>
        ///     The number of seconds between each tip being displayed when the round is actively going
        /// </summary>
        public static readonly CVarDef<float> TipFrequencyInRound =
            CVarDef.Create("tips.in_game_frequency", 60f * 60);

        public static readonly CVarDef<string> LoginTipsDataset =
            CVarDef.Create("tips.login_dataset", "Tips");

        /// <summary>
        ///     The chance for Tippy to replace a normal tip message.
        /// </summary>
        public static readonly CVarDef<float> TipsTippyChance =
            CVarDef.Create("tips.tippy_chance", 0.01f);

        /*
         * Console
         */

        public static readonly CVarDef<bool> ConsoleLoginLocal =
            CVarDef.Create("console.loginlocal", true, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Automatically log in the given user as host, equivalent to the <c>promotehost</c> command.
        /// </summary>
        public static readonly CVarDef<string> ConsoleLoginHostUser =
            CVarDef.Create("console.login_host_user", "", CVar.ARCHIVE | CVar.SERVERONLY);


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

        /// <summary>
        /// Amount of concurrent SQLite database operations.
        /// </summary>
        /// <remarks>
        /// Note that SQLite is not a properly asynchronous database and also has limited read/write concurrency.
        /// Increasing this number may allow more concurrent reads, but it probably won't matter much.
        /// SQLite operations are normally ran on the thread pool, which may cause thread pool starvation if the concurrency is too high.
        /// </remarks>
        public static readonly CVarDef<int> DatabaseSqliteConcurrency =
            CVarDef.Create("database.sqlite_concurrency", 3, CVar.SERVERONLY);

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
            CVarDef.Create("database.pg_username", "postgres", CVar.SERVERONLY);

        public static readonly CVarDef<string> DatabasePgPassword =
            CVarDef.Create("database.pg_password", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

        /// <summary>
        /// Max amount of concurrent Postgres database operations.
        /// </summary>
        public static readonly CVarDef<int> DatabasePgConcurrency =
            CVarDef.Create("database.pg_concurrency", 8, CVar.SERVERONLY);

        /// <summary>
        /// Milliseconds to asynchronously delay all PostgreSQL database operations with.
        /// </summary>
        /// <remarks>
        /// This is intended for performance testing. It works different from <see cref="DatabaseSqliteDelay"/>,
        /// as the lag is applied after acquiring the database lock.
        /// </remarks>
        public static readonly CVarDef<int> DatabasePgFakeLag =
            CVarDef.Create("database.pg_fake_lag", 0, CVar.SERVERONLY);

        // Basically only exists for integration tests to avoid race conditions.
        public static readonly CVarDef<bool> DatabaseSynchronous =
            CVarDef.Create("database.sync", false, CVar.SERVERONLY);

        /*
         * Interface
         */

        public static readonly CVarDef<string> UIClickSound =
            CVarDef.Create("interface.click_sound", "/Audio/UserInterface/click.ogg", CVar.REPLICATED);

        public static readonly CVarDef<string> UIHoverSound =
            CVarDef.Create("interface.hover_sound", "/Audio/UserInterface/hover.ogg", CVar.REPLICATED);

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
            CVarDef.Create("physics.relative_movement", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<float> TileFrictionModifier =
            CVarDef.Create("physics.tile_friction", 40.0f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<float> StopSpeed =
            CVarDef.Create("physics.stop_speed", 0.1f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Whether mobs can push objects like lockers.
        /// </summary>
        /// <remarks>
        /// Technically client doesn't need to know about it but this may prevent a bug in the distant future so it stays.
        /// </remarks>
        public static readonly CVarDef<bool> MobPushing =
            CVarDef.Create("physics.mob_pushing", false, CVar.REPLICATED | CVar.SERVER);

        /*
         * Music
         */

        public static readonly CVarDef<bool> LobbyMusicEnabled =
            CVarDef.Create("ambience.lobby_music_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<bool> EventMusicEnabled =
            CVarDef.Create("ambience.event_music_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * Specific Sounds
         */
        // Round  end sound (APC Destroyed)
        public static readonly CVarDef<bool> RestartSoundsEnabled =
            CVarDef.Create("ambience.restart_sounds_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);


        /*
         * Admin sounds
         */

        public static readonly CVarDef<bool> AdminSoundsEnabled =
            CVarDef.Create("audio.admin_sounds_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);
        public static readonly CVarDef<string> AdminChatSoundPath =
            CVarDef.Create("audio.admin_chat_sound_path", "/Audio/Items/pop.ogg", CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);
        public static readonly CVarDef<float> AdminChatSoundVolume =
            CVarDef.Create("audio.admin_chat_sound_volume", -5f, CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);
        public static readonly CVarDef<string> AHelpSound =
            CVarDef.Create("audio.ahelp_sound", "/Audio/Effects/adminhelp.ogg", CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * HUD
         */

        public static readonly CVarDef<int> HudTheme =
            CVarDef.Create("hud.theme", 0, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<bool> HudHeldItemShow =
            CVarDef.Create("hud.held_item_show", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<bool> CombatModeIndicatorsPointShow =
            CVarDef.Create("hud.combat_mode_indicators_point_show", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<bool> LoocAboveHeadShow =
            CVarDef.Create("hud.show_looc_above_head", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<float> HudHeldItemOffset =
            CVarDef.Create("hud.held_item_offset", 28f, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<bool> HudFpsCounterVisible =
            CVarDef.Create("hud.fps_counter_visible", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * NPCs
         */

        public static readonly CVarDef<int> NPCMaxUpdates =
            CVarDef.Create("npc.max_updates", 128);

        public static readonly CVarDef<bool> NPCEnabled = CVarDef.Create("npc.enabled", true);

        /// <summary>
        /// Should NPCs pathfind when steering. For debug purposes.
        /// </summary>
        public static readonly CVarDef<bool> NPCPathfinding = CVarDef.Create("npc.pathfinding", true);

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
         * Admin
         */

        public static readonly CVarDef<bool> AdminAnnounceLogin =
            CVarDef.Create("admin.announce_login", true, CVar.SERVERONLY);

        public static readonly CVarDef<bool> AdminAnnounceLogout =
            CVarDef.Create("admin.announce_logout", true, CVar.SERVERONLY);

        /// <summary>
        ///     The token used to authenticate with the admin API. Leave empty to disable the admin API. This is a secret! Do not share!
        /// </summary>
        public static readonly CVarDef<string> AdminApiToken =
            CVarDef.Create("admin.api_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);


        /// <summary>
        /// Should users be able to see their own notes? Admins will be able to see and set notes regardless
        /// </summary>
        public static readonly CVarDef<bool> SeeOwnNotes =
            CVarDef.Create("admin.see_own_notes", false, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Should the server play a quick sound to the active admins whenever a new player joins?
        /// </summary>
        public static readonly CVarDef<bool> AdminNewPlayerJoinSound =
            CVarDef.Create("admin.new_player_join_sound", false, CVar.SERVERONLY);

        /// <summary>
        /// The amount of days before the note starts fading. It will slowly lose opacity until it reaches stale. Set to 0 to disable.
        /// </summary>
        public static readonly CVarDef<double> NoteFreshDays =
            CVarDef.Create("admin.note_fresh_days", 91.31055, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// The amount of days before the note completely fades, and can only be seen by admins if they press "see more notes". Set to 0
        /// if you want the note to immediately disappear without fading.
        /// </summary>
        public static readonly CVarDef<double> NoteStaleDays =
            CVarDef.Create("admin.note_stale_days", 365.2422, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// How much time does the user have to wait in seconds before confirming that they saw an admin message?
        /// </summary>
        public static readonly CVarDef<float> MessageWaitTime =
            CVarDef.Create("admin.message_wait_time", 3f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Default severity for role bans
        /// </summary>
        public static readonly CVarDef<string> RoleBanDefaultSeverity =
            CVarDef.Create("admin.role_ban_default_severity", "medium", CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Default severity for department bans
        /// </summary>
        public static readonly CVarDef<string> DepartmentBanDefaultSeverity =
            CVarDef.Create("admin.department_ban_default_severity", "medium", CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Default severity for server bans
        /// </summary>
        public static readonly CVarDef<string> ServerBanDefaultSeverity =
            CVarDef.Create("admin.server_ban_default_severity", "High", CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Whether a server ban will ban the player's ip by default.
        /// </summary>
        public static readonly CVarDef<bool> ServerBanIpBanDefault =
            CVarDef.Create("admin.server_ban_ip_ban_default", true, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Whether a server ban will ban the player's hardware id by default.
        /// </summary>
        public static readonly CVarDef<bool> ServerBanHwidBanDefault =
            CVarDef.Create("admin.server_ban_hwid_ban_default", true, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Whether to use details from last connection for ip/hwid in the BanPanel.
        /// </summary>
        public static readonly CVarDef<bool> ServerBanUseLastDetails =
            CVarDef.Create("admin.server_ban_use_last_details", true, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Whether to erase a player's chat messages and their entity from the game when banned.
        /// </summary>
        public static readonly CVarDef<bool> ServerBanErasePlayer =
            CVarDef.Create("admin.server_ban_erase_player", false, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     Minimum players sharing a connection required to create an alert. -1 to disable the alert.
        /// </summary>
        /// <remarks>
        ///     If you set this to 0 or 1 then it will alert on every connection, so probably don't do that.
        /// </remarks>
        public static readonly CVarDef<int> AdminAlertMinPlayersSharingConnection =
            CVarDef.Create("admin.alert.min_players_sharing_connection", -1, CVar.SERVERONLY);

        /// <summary>
        ///     Minimum explosion intensity to create an admin alert message. -1 to disable the alert.
        /// </summary>
        public static readonly CVarDef<int> AdminAlertExplosionMinIntensity =
            CVarDef.Create("admin.alert.explosion_min_intensity", 60, CVar.SERVERONLY);

        /// <summary>
        ///     Minimum particle accelerator strength to create an admin alert message.
        /// </summary>
        public static readonly CVarDef<int> AdminAlertParticleAcceleratorMinPowerState =
            CVarDef.Create("admin.alert.particle_accelerator_min_power_state", 5, CVar.SERVERONLY); // strength 4

        /// <summary>
        ///     Should the ban details in admin channel include PII? (IP, HWID, etc)
        /// </summary>
        public static readonly CVarDef<bool> AdminShowPIIOnBan =
            CVarDef.Create("admin.show_pii_onban", false, CVar.SERVERONLY);

        /// <summary>
        /// If an admin joins a round by reading up or using the late join button, automatically
        /// de-admin them.
        /// </summary>
        public static readonly CVarDef<bool> AdminDeadminOnJoin =
            CVarDef.Create("admin.deadmin_on_join", false, CVar.SERVERONLY);

        /// <summary>
        ///     Overrides the name the client sees in ahelps. Set empty to disable.
        /// </summary>
        public static readonly CVarDef<string> AdminAhelpOverrideClientName =
            CVarDef.Create("admin.override_adminname_in_client_ahelp", string.Empty, CVar.SERVERONLY);

        /// <summary>
        ///     The threshold of minutes to appear as a "new player" in the ahelp menu
        ///     If 0, appearing as a new player is disabled.
        /// </summary>
        public static readonly CVarDef<int> NewPlayerThreshold =
            CVarDef.Create("admin.new_player_threshold", 0, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// How long an admin client can go without any input before being considered AFK.
        /// </summary>
        public static readonly CVarDef<float> AdminAfkTime =
            CVarDef.Create("admin.afk_time", 600f, CVar.SERVERONLY);

        /// <summary>
        /// If true, admins are able to connect even if
        /// <see cref="SoftMaxPlayers"/> would otherwise block regular players.
        /// </summary>
        public static readonly CVarDef<bool> AdminBypassMaxPlayers =
            CVarDef.Create("admin.bypass_max_players", true, CVar.SERVERONLY);

        /// <summary>
        /// Determine if custom rank names are used.
        /// If it is false, it'd use the actual rank name regardless of the individual's title.
        /// </summary>
        /// <seealso cref="AhelpAdminPrefix"/>
        /// <seealso cref="AhelpAdminPrefixWebhook"/>
        public static readonly CVarDef<bool> AdminUseCustomNamesAdminRank =
            CVarDef.Create("admin.use_custom_names_admin_rank", true, CVar.SERVERONLY);

        /*
         * AHELP
         */

        /// <summary>
        /// Ahelp rate limit values are accounted in periods of this size (seconds).
        /// After the period has passed, the count resets.
        /// </summary>
        /// <seealso cref="AhelpRateLimitCount"/>
        public static readonly CVarDef<int> AhelpRateLimitPeriod =
            CVarDef.Create("ahelp.rate_limit_period", 2, CVar.SERVERONLY);

        /// <summary>
        /// How many ahelp messages are allowed in a single rate limit period.
        /// </summary>
        /// <seealso cref="AhelpRateLimitPeriod"/>
        public static readonly CVarDef<int> AhelpRateLimitCount =
            CVarDef.Create("ahelp.rate_limit_count", 10, CVar.SERVERONLY);

        /// <summary>
        /// Should the administrator's position be displayed in ahelp.
        /// If it is is false, only the admin's ckey will be displayed in the ahelp.
        /// </summary>
        /// <seealso cref="AdminUseCustomNamesAdminRank"/>
        /// <seealso cref="AhelpAdminPrefixWebhook"/>
        public static readonly CVarDef<bool> AhelpAdminPrefix =
            CVarDef.Create("ahelp.admin_prefix", false, CVar.SERVERONLY);

        /// <summary>
        /// Should the administrator's position be displayed in the webhook.
        /// If it is is false, only the admin's ckey will be displayed in webhook.
        /// </summary>
        /// <seealso cref="AdminUseCustomNamesAdminRank"/>
        /// <seealso cref="AhelpAdminPrefix"/>
        public static readonly CVarDef<bool> AhelpAdminPrefixWebhook =
            CVarDef.Create("ahelp.admin_prefix_webhook", false, CVar.SERVERONLY);

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
            CVarDef.Create("explosion.max_area", (int) 3.14f * 256 * 256, CVar.SERVERONLY);

        /// <summary>
        ///     Upper limit on the number of neighbor finding steps for the explosion system neighbor-finding algorithm.
        /// </summary>
        /// <remarks>
        ///     Effectively places an upper limit on the range that any explosion can have. In the vast majority of
        ///     instances, <see cref="ExplosionMaxArea"/> will likely be hit before this becomes a limiting factor.
        /// </remarks>
        public static readonly CVarDef<int> ExplosionMaxIterations =
            CVarDef.Create("explosion.max_iterations", 500, CVar.SERVERONLY);

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
        ///     This determines for how many seconds an explosion should stay visible once it has finished expanding.
        /// </summary>
        public static readonly CVarDef<float> ExplosionPersistence =
            CVarDef.Create("explosion.persistence", 1.0f, CVar.SERVERONLY);

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

        /// <summary>
        ///     Whether or not explosions are allowed to create tiles that have
        ///     <see cref="ContentTileDefinition.MapAtmosphere"/> set to true.
        /// </summary>
        public static readonly CVarDef<bool> ExplosionCanCreateVacuum =
            CVarDef.Create("explosion.can_create_vacuum", true, CVar.SERVERONLY);

        /*
         * Radiation
         */

        /// <summary>
        ///     What is the smallest radiation dose in rads that can be received by object.
        ///     Extremely small values may impact performance.
        /// </summary>
        public static readonly CVarDef<float> RadiationMinIntensity =
            CVarDef.Create("radiation.min_intensity", 0.1f, CVar.SERVERONLY);

        /// <summary>
        ///     Rate of radiation system update in seconds.
        /// </summary>
        public static readonly CVarDef<float> RadiationGridcastUpdateRate =
            CVarDef.Create("radiation.gridcast.update_rate", 1.0f, CVar.SERVERONLY);

        /// <summary>
        ///     If both radiation source and receiver are placed on same grid, ignore grids between them.
        ///     May get inaccurate result in some cases, but greatly boost performance in general.
        /// </summary>
        public static readonly CVarDef<bool> RadiationGridcastSimplifiedSameGrid =
            CVarDef.Create("radiation.gridcast.simplified_same_grid", true, CVar.SERVERONLY);

        /// <summary>
        ///     Max distance that radiation ray can travel in meters.
        /// </summary>
        public static readonly CVarDef<float> RadiationGridcastMaxDistance =
            CVarDef.Create("radiation.gridcast.max_distance", 50f, CVar.SERVERONLY);

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

        // When to skip the waiting time to save in-round admin logs, if no admin logs are currently being saved
        public static readonly CVarDef<int> AdminLogsQueueMax =
            CVarDef.Create("adminlogs.queue_max", 5000, CVar.SERVERONLY);

        // When to skip the waiting time to save pre-round admin logs, if no admin logs are currently being saved
        public static readonly CVarDef<int> AdminLogsPreRoundQueueMax =
            CVarDef.Create("adminlogs.pre_round_queue_max", 5000, CVar.SERVERONLY);

        // When to start dropping logs
        public static readonly CVarDef<int> AdminLogsDropThreshold =
            CVarDef.Create("adminlogs.drop_threshold", 20000, CVar.SERVERONLY);

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
            CVarDef.Create("atmos.monstermos_depressurization", true, CVar.SERVERONLY);

        /// <summary>
        ///     Whether monstermos explosive depressurization will rip tiles..
        ///     Needs <see cref="MonstermosEqualization"/> and <see cref="MonstermosDepressurization"/> to be enabled to work.
		///     WARNING: This cvar causes MAJOR contrast issues, and usually tends to make any spaced scene look very cluttered.
		///     This not only usually looks strange, but can also reduce playability for people with impaired vision. Please think twice before enabling this on your server.
		///     Also looks weird on slow spacing for unrelated reasons. If you do want to enable this, you should probably turn on instaspacing.
        /// </summary>
        public static readonly CVarDef<bool> MonstermosRipTiles =
            CVarDef.Create("atmos.monstermos_rip_tiles", false, CVar.SERVERONLY);

        /// <summary>
        ///     Whether explosive depressurization will cause the grid to gain an impulse.
        ///     Needs <see cref="MonstermosEqualization"/> and <see cref="MonstermosDepressurization"/> to be enabled to work.
        /// </summary>
        public static readonly CVarDef<bool> AtmosGridImpulse =
            CVarDef.Create("atmos.grid_impulse", false, CVar.SERVERONLY);

        /// <summary>
        ///     What fraction of air from a spaced tile escapes every tick.
        ///     1.0 for instant spacing, 0.2 means 20% of remaining air lost each time
        /// </summary>
        public static readonly CVarDef<float> AtmosSpacingEscapeRatio =
            CVarDef.Create("atmos.mmos_spacing_speed", 0.15f, CVar.SERVERONLY);

        /// <summary>
        ///     Minimum amount of air allowed on a spaced tile before it is reset to 0 immediately in kPa
        ///     Since the decay due to SpacingEscapeRatio follows a curve, it would never reach 0.0 exactly
        ///     unless we truncate it somewhere.
        /// </summary>
        public static readonly CVarDef<float> AtmosSpacingMinGas =
            CVarDef.Create("atmos.mmos_min_gas", 2.0f, CVar.SERVERONLY);

        /// <summary>
        ///     How much wind can go through a single tile before that tile doesn't depressurize itself
        ///     (I.e spacing is limited in large rooms heading into smaller spaces)
        /// </summary>
        public static readonly CVarDef<float> AtmosSpacingMaxWind =
            CVarDef.Create("atmos.mmos_max_wind", 500f, CVar.SERVERONLY);

        /// <summary>
        ///     Whether atmos superconduction is enabled.
        /// </summary>
        /// <remarks> Disabled by default, superconduction is awful. </remarks>
        public static readonly CVarDef<bool> Superconduction =
            CVarDef.Create("atmos.superconduction", false, CVar.SERVERONLY);

        /// <summary>
        ///     Heat loss per tile due to radiation at 20 degC, in W.
        /// </summary>
        public static readonly CVarDef<float> SuperconductionTileLoss =
            CVarDef.Create("atmos.superconduction_tile_loss", 30f, CVar.SERVERONLY);

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

        /// <summary>
        ///     Scale factor for how fast things happen in our atmosphere
        ///     simulation compared to real life. 1x means pumps run at 1x
        ///     speed. Players typically expect things to happen faster
        ///     in-game.
        /// </summary>
        public static readonly CVarDef<float> AtmosSpeedup =
            CVarDef.Create("atmos.speedup", 8f, CVar.SERVERONLY);

        /// <summary>
        ///     Like atmos.speedup, but only for gas and reaction heat values. 64x means
        ///     gases heat up and cool down 64x faster than real life.
        /// </summary>
        public static readonly CVarDef<float> AtmosHeatScale =
            CVarDef.Create("atmos.heat_scale", 8f, CVar.SERVERONLY);

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

        /// <summary>
        /// Whether or not OOC chat should be enabled during a round.
        /// </summary>
        public static readonly CVarDef<bool> OocEnableDuringRound =
            CVarDef.Create("ooc.enable_during_round", false, CVar.NOTIFY | CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<bool> ShowOocPatronColor =
            CVarDef.Create("ooc.show_ooc_patron_color", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.CLIENT);

        /*
         * LOOC
         */

        public static readonly CVarDef<bool> LoocEnabled = CVarDef.Create("looc.enabled", true, CVar.NOTIFY | CVar.REPLICATED);

        public static readonly CVarDef<bool> AdminLoocEnabled =
            CVarDef.Create("looc.enabled_admin", true, CVar.NOTIFY);

        /// <summary>
        /// True: Dead players can use LOOC
        /// False: Dead player LOOC gets redirected to dead chat
        /// </summary>
        public static readonly CVarDef<bool> DeadLoocEnabled = CVarDef.Create("looc.enabled_dead", false, CVar.NOTIFY | CVar.REPLICATED);

        /// <summary>
        /// True: Crit players can use LOOC
        /// False: Crit player LOOC gets redirected to dead chat
        /// </summary>
        public static readonly CVarDef<bool> CritLoocEnabled = CVarDef.Create("looc.enabled_crit", false, CVar.NOTIFY | CVar.REPLICATED);

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
        /// <summary>
        ///     Specifies the whitelist prototypes to be used by the server. This should be a comma-separated list of prototypes.
        ///     If a whitelists conditions to be active fail (for example player count), the next whitelist will be used instead. If no whitelist is valid, the player will be allowed to connect.
        /// </summary>
        public static readonly CVarDef<string> WhitelistPrototypeList =
            CVarDef.Create("whitelist.prototype_list", "basicWhitelist", CVar.SERVERONLY);

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
        ///     Config for when the restart vote should be allowed to be called regardless with less than this amount of players.
        /// </summary>
        public static readonly CVarDef<int> VoteRestartMaxPlayers =
            CVarDef.Create("vote.restart_max_players", 20, CVar.SERVERONLY);

        /// <summary>
        ///     Config for when the restart vote should be allowed to be called based on percentage of ghosts.
        ///
        public static readonly CVarDef<int> VoteRestartGhostPercentage =
            CVarDef.Create("vote.restart_ghost_percentage", 55, CVar.SERVERONLY);

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
            CVarDef.Create("vote.restart_required_ratio", 0.85f, CVar.SERVERONLY);

        /// <summary>
        /// Whether or not to prevent the restart vote from having any effect when there is an online admin
        /// </summary>
        public static readonly CVarDef<bool> VoteRestartNotAllowedWhenAdminOnline =
            CVarDef.Create("vote.restart_not_allowed_when_admin_online", true, CVar.SERVERONLY);

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
         * Procgen
         */

        /// <summary>
        /// Should we pre-load all of the procgen atlasses.
        /// </summary>
        public static readonly CVarDef<bool> ProcgenPreload =
            CVarDef.Create("procgen.preload", true, CVar.SERVERONLY);

        /*
         * Shuttles
         */

        // Look this is technically eye behavior but its main impact is shuttles so I just dumped it here.
        /// <summary>
        /// If true then the camera will match the grid / map and is unchangeable.
        /// - When traversing grids it will snap to 0 degrees rotation.
        /// False means the player has control over the camera rotation.
        /// - When traversing grids it will snap to the nearest cardinal which will generally be imperceptible.
        /// </summary>
        public static readonly CVarDef<bool> CameraRotationLocked =
            CVarDef.Create("shuttle.camera_rotation_locked", false, CVar.REPLICATED);

        /// <summary>
        /// Whether the arrivals terminal should be on a planet map.
        /// </summary>
        public static readonly CVarDef<bool> ArrivalsPlanet =
            CVarDef.Create("shuttle.arrivals_planet", true, CVar.SERVERONLY);

        /// <summary>
        /// Whether the arrivals shuttle is enabled.
        /// </summary>
        public static readonly CVarDef<bool> ArrivalsShuttles =
            CVarDef.Create("shuttle.arrivals", true, CVar.SERVERONLY);

        /// <summary>
        /// The map to use for the arrivals station.
        /// </summary>
        public static readonly CVarDef<string> ArrivalsMap =
            CVarDef.Create("shuttle.arrivals_map", "/Maps/Misc/terminal.yml", CVar.SERVERONLY);

        /// <summary>
        /// Cooldown between arrivals departures. This should be longer than the FTL time or it will double cycle.
        /// </summary>
        public static readonly CVarDef<float> ArrivalsCooldown =
            CVarDef.Create("shuttle.arrivals_cooldown", 50f, CVar.SERVERONLY);

        /// <summary>
        /// Are players allowed to return on the arrivals shuttle.
        /// </summary>
        public static readonly CVarDef<bool> ArrivalsReturns =
            CVarDef.Create("shuttle.arrivals_returns", false, CVar.SERVERONLY);

        /// <summary>
        /// Should all players who spawn at arrivals have godmode until they leave the map?
        /// </summary>
        public static readonly CVarDef<bool> GodmodeArrivals =
            CVarDef.Create("shuttle.godmode_arrivals", false, CVar.SERVERONLY);

        /// <summary>
        /// If a grid is split then hide any smaller ones under this mass (kg) from the map.
        /// This is useful to avoid split grids spamming out labels.
        /// </summary>
        public static readonly CVarDef<int> HideSplitGridsUnder =
            CVarDef.Create("shuttle.hide_split_grids_under", 30, CVar.SERVERONLY);

        /// <summary>
        /// Whether to automatically spawn escape shuttles.
        /// </summary>
        public static readonly CVarDef<bool> GridFill =
            CVarDef.Create("shuttle.grid_fill", true, CVar.SERVERONLY);

        /// <summary>
        /// Whether to automatically preloading grids by GridPreloaderSystem
        /// </summary>
        public static readonly CVarDef<bool> PreloadGrids =
            CVarDef.Create("shuttle.preload_grids", true, CVar.SERVERONLY);

        /// <summary>
        /// How long the warmup time before FTL start should be.
        /// </summary>
        public static readonly CVarDef<float> FTLStartupTime =
            CVarDef.Create("shuttle.startup_time", 5.5f, CVar.SERVERONLY);

        /// <summary>
        /// How long a shuttle spends in FTL.
        /// </summary>
        public static readonly CVarDef<float> FTLTravelTime =
            CVarDef.Create("shuttle.travel_time", 20f, CVar.SERVERONLY);

        /// <summary>
        /// How long the final stage of FTL before arrival should be.
        /// </summary>
        public static readonly CVarDef<float> FTLArrivalTime =
            CVarDef.Create("shuttle.arrival_time", 5f, CVar.SERVERONLY);

        /// <summary>
        /// How much time needs to pass before a shuttle can FTL again.
        /// </summary>
        public static readonly CVarDef<float> FTLCooldown =
            CVarDef.Create("shuttle.cooldown", 10f, CVar.SERVERONLY);

        /// <summary>
        /// The maximum <see cref="PhysicsComponent.Mass"/> a grid can have before it becomes unable to FTL.
        /// Any value equal to or less than zero will disable this check.
        /// </summary>
        public static readonly CVarDef<float> FTLMassLimit =
            CVarDef.Create("shuttle.mass_limit", 300f, CVar.SERVERONLY);

        /// <summary>
        /// How long to knock down entities for if they aren't buckled when FTL starts and stops.
        /// </summary>
        public static readonly CVarDef<float> HyperspaceKnockdownTime =
            CVarDef.Create("shuttle.hyperspace_knockdown_time", 5f, CVar.SERVERONLY);

        /*
         * Emergency
         */

        /// <summary>
        /// Is the emergency shuttle allowed to be early launched.
        /// </summary>
        public static readonly CVarDef<bool> EmergencyEarlyLaunchAllowed =
            CVarDef.Create("shuttle.emergency_early_launch_allowed", false, CVar.SERVERONLY);

        /// <summary>
        /// How long the emergency shuttle remains docked with the station, in seconds.
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleDockTime =
            CVarDef.Create("shuttle.emergency_dock_time", 180f, CVar.SERVERONLY);

        /// <summary>
        /// How long after the console is authorized for the shuttle to early launch.
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleAuthorizeTime =
            CVarDef.Create("shuttle.emergency_authorize_time", 10f, CVar.SERVERONLY);

        /// <summary>
        /// The minimum time for the emergency shuttle to arrive at centcomm.
        /// Actual minimum travel time cannot be less than <see cref="ShuttleSystem.DefaultArrivalTime"/>
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleMinTransitTime =
            CVarDef.Create("shuttle.emergency_transit_time_min", 60f, CVar.SERVERONLY);

        /// <summary>
        /// The maximum time for the emergency shuttle to arrive at centcomm.
        /// </summary>
        public static readonly CVarDef<float> EmergencyShuttleMaxTransitTime =
            CVarDef.Create("shuttle.emergency_transit_time_max", 180f, CVar.SERVERONLY);

        /// <summary>
        /// Whether the emergency shuttle is enabled or should the round just end.
        /// </summary>
        public static readonly CVarDef<bool> EmergencyShuttleEnabled =
            CVarDef.Create("shuttle.emergency", true, CVar.SERVERONLY);

        /// <summary>
        ///     The percentage of time passed from the initial call to when the shuttle can no longer be recalled.
        ///     ex. a call time of 10min and turning point of 0.5 means the shuttle cannot be recalled after 5 minutes.
        /// </summary>
        public static readonly CVarDef<float> EmergencyRecallTurningPoint =
            CVarDef.Create("shuttle.recall_turning_point", 0.5f, CVar.SERVERONLY);

        /// <summary>
        ///     Time in minutes after round start to auto-call the shuttle. Set to zero to disable.
        /// </summary>
        public static readonly CVarDef<int> EmergencyShuttleAutoCallTime =
            CVarDef.Create("shuttle.auto_call_time", 90, CVar.SERVERONLY);

        /// <summary>
        ///     Time in minutes after the round was extended (by recalling the shuttle) to call
        ///     the shuttle again.
        /// </summary>
        public static readonly CVarDef<int> EmergencyShuttleAutoCallExtensionTime =
            CVarDef.Create("shuttle.auto_call_extension_time", 45, CVar.SERVERONLY);

        /*
         * Crew Manifests
         */

        /// <summary>
        ///     Setting this allows a crew manifest to be opened from any window
        ///     that has a crew manifest button, and sends the correct message.
        ///     If this is false, only in-game entities will allow you to see
        ///     the crew manifest, if the functionality is coded in.
        ///     Having administrator priveledge ignores this, but will still
        ///     hide the button in UI windows.
        /// </summary>
        public static readonly CVarDef<bool> CrewManifestWithoutEntity =
            CVarDef.Create("crewmanifest.no_entity", true, CVar.REPLICATED);

        /// <summary>
        ///     Setting this allows the crew manifest to be viewed from 'unsecure'
        ///     entities, such as the PDA.
        /// </summary>
        public static readonly CVarDef<bool> CrewManifestUnsecure =
            CVarDef.Create("crewmanifest.unsecure", true, CVar.REPLICATED);

        /*
         * Biomass
         */

        /// <summary>
        ///     Enabled: Cloning has 70% cost and reclaimer will refuse to reclaim corpses with souls. (For LRP).
        ///     Disabled: Cloning has full biomass cost and reclaimer can reclaim corpses with souls. (Playtested and balanced for MRP+).
        /// </summary>
        public static readonly CVarDef<bool> BiomassEasyMode =
            CVarDef.Create("biomass.easy_mode", true, CVar.SERVERONLY);

        /*
         * Anomaly
         */

        /// <summary>
        ///     A scale factor applied to a grid's bounds when trying to find a spot to randomly generate an anomaly.
        /// </summary>
        public static readonly CVarDef<float> AnomalyGenerationGridBoundsScale =
            CVarDef.Create("anomaly.generation_grid_bounds_scale", 0.6f, CVar.SERVERONLY);

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

        public static readonly CVarDef<int> ViewportMinimumWidth =
            CVarDef.Create("viewport.minimum_width", 15, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> ViewportMaximumWidth =
            CVarDef.Create("viewport.maximum_width", 21, CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<int> ViewportWidth =
            CVarDef.Create("viewport.width", 21, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<bool> ViewportVerticalFit =
            CVarDef.Create("viewport.vertical_fit", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * UI
         */

        public static readonly CVarDef<string> UILayout =
            CVarDef.Create("ui.layout", "Default", CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<string> DefaultScreenChatSize =
            CVarDef.Create("ui.default_chat_size", "", CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<string> SeparatedScreenChatSize =
            CVarDef.Create("ui.separated_chat_size", "0.6,0", CVar.CLIENTONLY | CVar.ARCHIVE);


        /*
        * Accessibility
        */

        /// <summary>
        /// Chat window opacity slider, controlling the alpha of the chat window background.
        /// Goes from to 0 (completely transparent) to 1 (completely opaque)
        /// </summary>
        public static readonly CVarDef<float> ChatWindowOpacity =
            CVarDef.Create("accessibility.chat_window_transparency", 0.85f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Toggle for visual effects that may potentially cause motion sickness.
        /// Where reasonable, effects affected by this CVar should use an alternate effect.
        /// Please do not use this CVar as a bandaid for effects that could otherwise be made accessible without issue.
        /// </summary>
        public static readonly CVarDef<bool> ReducedMotion =
            CVarDef.Create("accessibility.reduced_motion", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<bool> ChatEnableColorName =
            CVarDef.Create("accessibility.enable_color_name", true, CVar.CLIENTONLY | CVar.ARCHIVE, "Toggles displaying names with individual colors.");

        /// <summary>
        /// Screen shake intensity slider, controlling the intensity of the CameraRecoilSystem.
        /// Goes from 0 (no recoil at all) to 1 (regular amounts of recoil)
        /// </summary>
        public static readonly CVarDef<float> ScreenShakeIntensity =
            CVarDef.Create("accessibility.screen_shake_intensity", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// A generic toggle for various visual effects that are color sensitive.
        /// As of 2/16/24, only applies to progress bar colors.
        /// </summary>
        public static readonly CVarDef<bool> AccessibilityColorblindFriendly =
            CVarDef.Create("accessibility.colorblind_friendly", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * CHAT
         */

        /// <summary>
        /// Chat rate limit values are accounted in periods of this size (seconds).
        /// After the period has passed, the count resets.
        /// </summary>
        /// <seealso cref="ChatRateLimitCount"/>
        public static readonly CVarDef<int> ChatRateLimitPeriod =
            CVarDef.Create("chat.rate_limit_period", 2, CVar.SERVERONLY);

        /// <summary>
        /// How many chat messages are allowed in a single rate limit period.
        /// </summary>
        /// <remarks>
        /// The total rate limit throughput per second is effectively
        /// <see cref="ChatRateLimitCount"/> divided by <see cref="ChatRateLimitCount"/>.
        /// </remarks>
        /// <seealso cref="ChatRateLimitPeriod"/>
        /// <seealso cref="ChatRateLimitAnnounceAdmins"/>
        public static readonly CVarDef<int> ChatRateLimitCount =
            CVarDef.Create("chat.rate_limit_count", 10, CVar.SERVERONLY);

        /// <summary>
        /// If true, announce when a player breached chat rate limit to game administrators.
        /// </summary>
        /// <seealso cref="ChatRateLimitAnnounceAdminsDelay"/>
        public static readonly CVarDef<bool> ChatRateLimitAnnounceAdmins =
            CVarDef.Create("chat.rate_limit_announce_admins", true, CVar.SERVERONLY);

        /// <summary>
        /// Minimum delay (in seconds) between announcements from <see cref="ChatRateLimitAnnounceAdmins"/>.
        /// </summary>
        public static readonly CVarDef<int> ChatRateLimitAnnounceAdminsDelay =
            CVarDef.Create("chat.rate_limit_announce_admins_delay", 15, CVar.SERVERONLY);

        public static readonly CVarDef<int> ChatMaxMessageLength =
            CVarDef.Create("chat.max_message_length", 1000, CVar.SERVER | CVar.REPLICATED);

        public static readonly CVarDef<int> ChatMaxAnnouncementLength =
            CVarDef.Create("chat.max_announcement_length", 256, CVar.SERVER | CVar.REPLICATED);

        public static readonly CVarDef<bool> ChatSanitizerEnabled =
            CVarDef.Create("chat.chat_sanitizer_enabled", true, CVar.SERVERONLY);

        public static readonly CVarDef<bool> ChatShowTypingIndicator =
            CVarDef.Create("chat.show_typing_indicator", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<bool> ChatEnableFancyBubbles =
            CVarDef.Create("chat.enable_fancy_bubbles", true, CVar.CLIENTONLY | CVar.ARCHIVE, "Toggles displaying fancy speech bubbles, which display the speaking character's name.");

        public static readonly CVarDef<bool> ChatFancyNameBackground =
            CVarDef.Create("chat.fancy_name_background", false, CVar.CLIENTONLY | CVar.ARCHIVE, "Toggles displaying a background under the speaking character's name.");

        /// <summary>
        /// A message broadcast to each player that joins the lobby.
        /// May be changed by admins ingame through use of the "set-motd" command.
        /// In this case the new value, if not empty, is broadcast to all connected players and saved between rounds.
        /// May be requested by any player through use of the "get-motd" command.
        /// </summary>
        public static readonly CVarDef<string> MOTD =
            CVarDef.Create("chat.motd", "", CVar.SERVER | CVar.SERVERONLY | CVar.ARCHIVE, "A message broadcast to each player that joins the lobby.");

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

        /// <summary>
        /// Adds a period at the end of a sentence if the sentence ends in a letter.
        /// </summary>
        public static readonly CVarDef<bool> ChatPunctuation =
            CVarDef.Create("ic.punctuation", false, CVar.SERVER);

        /// <summary>
        /// Enables automatically forcing IC name rules. Uppercases the first letter of the first and last words of the name
        /// </summary>
        public static readonly CVarDef<bool> ICNameCase =
            CVarDef.Create("ic.name_case", true, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Whether or not players' characters are randomly generated rather than using their selected characters in the creator.
        /// </summary>
        public static readonly CVarDef<bool> ICRandomCharacters =
            CVarDef.Create("ic.random_characters", false, CVar.SERVER);

        /// <summary>
        /// A weighted random prototype used to determine the species selected for random characters.
        /// </summary>
        public static readonly CVarDef<string> ICRandomSpeciesWeights =
            CVarDef.Create("ic.random_species_weights", "SpeciesWeights", CVar.SERVER);

        /// <summary>
        /// Control displaying SSD indicators near players
        /// </summary>
        public static readonly CVarDef<bool> ICShowSSDIndicator =
            CVarDef.Create("ic.show_ssd_indicator", true, CVar.CLIENTONLY);

        /*
         * Salvage
         */

        /// <summary>
        /// Duration for missions
        /// </summary>
        public static readonly CVarDef<float>
            SalvageExpeditionDuration = CVarDef.Create("salvage.expedition_duration", 660f, CVar.REPLICATED);

        /// <summary>
        /// Cooldown for missions.
        /// </summary>
        public static readonly CVarDef<float>
            SalvageExpeditionCooldown = CVarDef.Create("salvage.expedition_cooldown", 780f, CVar.REPLICATED);

        /*
         * Flavor
         */

        /// <summary>
        ///     Flavor limit. This is to ensure that having a large mass of flavors in
        ///     some food object won't spam a user with flavors.
        /// </summary>
        public static readonly CVarDef<int>
            FlavorLimit = CVarDef.Create("flavor.limit", 10, CVar.SERVERONLY);

        /*
         * Mapping
         */

        /// <summary>
        ///     Will mapping mode enable autosaves when it's activated?
        /// </summary>
        public static readonly CVarDef<bool>
            AutosaveEnabled = CVarDef.Create("mapping.autosave", true, CVar.SERVERONLY);

        /// <summary>
        ///     Autosave interval in seconds.
        /// </summary>
        public static readonly CVarDef<float>
            AutosaveInterval = CVarDef.Create("mapping.autosave_interval", 600f, CVar.SERVERONLY);

        /// <summary>
        ///     Directory in server user data to save to. Saves will be inside folders in this directory.
        /// </summary>
        public static readonly CVarDef<string>
            AutosaveDirectory = CVarDef.Create("mapping.autosave_dir", "Autosaves", CVar.SERVERONLY);


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

        /// <summary>
        /// Toggles whether the walking key is a toggle or a held key.
        /// </summary>
        public static readonly CVarDef<bool> ToggleWalk =
            CVarDef.Create("control.toggle_walk", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * STORAGE
         */

        /// <summary>
        /// Whether or not the storage UI is static and bound to the hotbar, or unbound and allowed to be dragged anywhere.
        /// </summary>
        public static readonly CVarDef<bool> StaticStorageUI =
            CVarDef.Create("control.static_storage_ui", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Whether or not the storage window uses a transparent or opaque sprite.
        /// </summary>
        public static readonly CVarDef<bool> OpaqueStorageWindow =
            CVarDef.Create("control.opaque_storage_background", false, CVar.CLIENTONLY | CVar.ARCHIVE);

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
            CVarDef.Create("ghost.role_time", 3f, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// If ghost role lotteries should be made near-instanteous.
        /// </summary>
        public static readonly CVarDef<bool> GhostQuickLottery =
            CVarDef.Create("ghost.quick_lottery", false, CVar.SERVERONLY);

        /// <summary>
        /// Whether or not to kill the player's mob on ghosting, when it is in a critical health state.
        /// </summary>
        public static readonly CVarDef<bool> GhostKillCrit =
            CVarDef.Create("ghost.kill_crit", true, CVar.REPLICATED | CVar.SERVER);

        /*
         * Fire alarm
         */

        /// <summary>
        ///     If fire alarms should have all access, or if activating/resetting these
        ///     should be restricted to what is dictated on a player's access card.
        ///     Defaults to true.
        /// </summary>
        public static readonly CVarDef<bool> FireAlarmAllAccess =
            CVarDef.Create("firealarm.allaccess", true, CVar.SERVERONLY);

        /*
         * PLAYTIME
         */

        /// <summary>
        /// Time between play time autosaves, in seconds.
        /// </summary>
        public static readonly CVarDef<float>
            PlayTimeSaveInterval = CVarDef.Create("playtime.save_interval", 900f, CVar.SERVERONLY);

        /*
         * INFOLINKS
         */

        /// <summary>
        /// Link to Discord server to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksDiscord =
            CVarDef.Create("infolinks.discord", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to website to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksForum =
            CVarDef.Create("infolinks.forum", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to GitHub page to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksGithub =
            CVarDef.Create("infolinks.github", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to website to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksWebsite =
            CVarDef.Create("infolinks.website", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to wiki to show in the launcher.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksWiki =
            CVarDef.Create("infolinks.wiki", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to Patreon. Not shown in the launcher currently.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksPatreon =
            CVarDef.Create("infolinks.patreon", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to the bug report form.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksBugReport =
            CVarDef.Create("infolinks.bug_report", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// Link to site handling ban appeals. Shown in ban disconnect messages.
        /// </summary>
        public static readonly CVarDef<string> InfoLinksAppeal =
            CVarDef.Create("infolinks.appeal", "", CVar.SERVER | CVar.REPLICATED);

        /*
         * CONFIG
         */

        // These are server-only for now since I don't foresee a client use yet,
        // and I don't wanna have to start coming up with like .client suffixes and stuff like that.

        /// <summary>
        /// Configuration presets to load during startup.
        /// Multiple presets can be separated by comma and are loaded in order.
        /// </summary>
        /// <remarks>
        /// Loaded presets must be located under the <c>ConfigPresets/</c> resource directory and end with the <c>.toml</c> extension.
        /// Only the file name (without extension) must be given for this variable.
        /// </remarks>
        public static readonly CVarDef<string> ConfigPresets =
            CVarDef.Create("config.presets", "", CVar.SERVERONLY);

        /// <summary>
        /// Whether to load the preset development CVars.
        /// This disables some things like lobby to make development easier.
        /// Even when true, these are only loaded if the game is compiled with <c>DEVELOPMENT</c> set.
        /// </summary>
        public static readonly CVarDef<bool> ConfigPresetDevelopment =
            CVarDef.Create("config.preset_development", true, CVar.SERVERONLY);

        /// <summary>
        /// Whether to load the preset debug CVars.
        /// Even when true, these are only loaded if the game is compiled with <c>DEBUG</c> set.
        /// </summary>
        public static readonly CVarDef<bool> ConfigPresetDebug =
            CVarDef.Create("config.preset_debug", true, CVar.SERVERONLY);

        /*
         * World Generation
         */
        /// <summary>
        ///     Whether or not world generation is enabled.
        /// </summary>
        public static readonly CVarDef<bool> WorldgenEnabled =
            CVarDef.Create("worldgen.enabled", false, CVar.SERVERONLY);

        /// <summary>
        ///     The worldgen config to use.
        /// </summary>
        public static readonly CVarDef<string> WorldgenConfig =
            CVarDef.Create("worldgen.worldgen_config", "Default", CVar.SERVERONLY);

        /// <summary>
        ///     The maximum amount of time the entity GC can process, in ms.
        /// </summary>
        public static readonly CVarDef<int> GCMaximumTimeMs =
            CVarDef.Create("entgc.maximum_time_ms", 5, CVar.SERVERONLY);

        /*
         * Replays
         */

        /// <summary>
        ///     Whether or not to record admin chat. If replays are being publicly distributes, this should probably be
        ///     false.
        /// </summary>
        public static readonly CVarDef<bool> ReplayRecordAdminChat =
            CVarDef.Create("replay.record_admin_chat", false, CVar.ARCHIVE);

        /// <summary>
        /// Automatically record full rounds as replays.
        /// </summary>
        public static readonly CVarDef<bool> ReplayAutoRecord =
            CVarDef.Create("replay.auto_record", false, CVar.SERVERONLY);

        /// <summary>
        /// The file name to record automatic replays to. The path is relative to <see cref="CVars.ReplayDirectory"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the path includes slashes, directories will be automatically created if necessary.
        /// </para>
        /// <para>
        /// A number of substitutions can be used to automatically fill in the file name: <c>{year}</c>, <c>{month}</c>, <c>{day}</c>, <c>{hour}</c>, <c>{minute}</c>, <c>{round}</c>.
        /// </para>
        /// </remarks>
        public static readonly CVarDef<string> ReplayAutoRecordName =
            CVarDef.Create("replay.auto_record_name", "{year}_{month}_{day}-{hour}_{minute}-round_{round}.zip", CVar.SERVERONLY);

        /// <summary>
        /// Path that, if provided, automatic replays are initially recorded in.
        /// When the recording is done, the file is moved into its final destination.
        /// Unless this path is rooted, it will be relative to <see cref="CVars.ReplayDirectory"/>.
        /// </summary>
        public static readonly CVarDef<string> ReplayAutoRecordTempDir =
            CVarDef.Create("replay.auto_record_temp_dir", "", CVar.SERVERONLY);

        /*
         * Miscellaneous
         */

        public static readonly CVarDef<bool> GatewayGeneratorEnabled =
            CVarDef.Create("gateway.generator_enabled", true);

        // Clippy!
        public static readonly CVarDef<string> TippyEntity =
            CVarDef.Create("tippy.entity", "Tippy", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     The number of seconds that must pass for a single entity to be able to point at something again.
        /// </summary>
        public static readonly CVarDef<float> PointingCooldownSeconds =
            CVarDef.Create("pointing.cooldown_seconds", 0.5f, CVar.SERVERONLY);

        /*
         * DEBUG
         */

        /// <summary>
        /// A simple toggle to test <c>OptionsVisualizerComponent</c>.
        /// </summary>
        public static readonly CVarDef<bool> DebugOptionVisualizerTest =
            CVarDef.Create("debug.option_visualizer_test", false, CVar.CLIENTONLY);

        /// <summary>
        /// Set to true to disable parallel processing in the pow3r solver.
        /// </summary>
        public static readonly CVarDef<bool> DebugPow3rDisableParallel =
            CVarDef.Create("debug.pow3r_disable_parallel", true, CVar.SERVERONLY);
    }
}
