using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
        /// <summary>
        ///     How long we'll wait until re-sampling nearby objects for ambience. Should be pretty fast, but doesn't have to match the tick rate.
        /// </summary>
        public static readonly CVarDef<float> AmbientCooldown =
            CVarDef.Create("ambience.cooldown", 0.1f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        ///     How large of a range to sample for ambience.
        /// </summary>
        public static readonly CVarDef<float> AmbientRange =
            CVarDef.Create("ambience.range", 8f, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        ///     Maximum simultaneous ambient sounds.
        /// </summary>
        public static readonly CVarDef<int> MaxAmbientSources =
            CVarDef.Create("ambience.max_sounds", 16, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        ///     The minimum value the user can set for ambience.max_sounds
        /// </summary>
        public static readonly CVarDef<int> MinMaxAmbientSourcesConfigured =
            CVarDef.Create("ambience.min_max_sounds_configured", 16, CVar.REPLICATED | CVar.SERVER | CVar.CHEAT);

        /// <summary>
        ///     The maximum value the user can set for ambience.max_sounds
        /// </summary>
        public static readonly CVarDef<int> MaxMaxAmbientSourcesConfigured =
            CVarDef.Create("ambience.max_max_sounds_configured", 64, CVar.REPLICATED | CVar.SERVER | CVar.CHEAT);

        /// <summary>
        ///     Ambience volume.
        /// </summary>
        public static readonly CVarDef<float> AmbienceVolume =
            CVarDef.Create("ambience.volume", 1.5f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        ///     Ambience music volume.
        /// </summary>
        public static readonly CVarDef<float> AmbientMusicVolume =
            CVarDef.Create("ambience.music_volume", 1.5f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        ///     Lobby / round end music volume.
        /// </summary>
        public static readonly CVarDef<float> LobbyMusicVolume =
            CVarDef.Create("ambience.lobby_music_volume", 0.50f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        ///     UI volume.
        /// </summary>
        public static readonly CVarDef<float> InterfaceVolume =
            CVarDef.Create("audio.interface_volume", 0.50f, CVar.ARCHIVE | CVar.CLIENTONLY);

}
