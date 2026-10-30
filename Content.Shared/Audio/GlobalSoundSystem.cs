using Content.Shared.CCVar;
using Content.Shared.Station.Systems;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio;

/// <summary>
/// Handles playing audio to all players globally unless disabled by cvar. Some events are grid-specific.
/// </summary>
public abstract partial class GlobalSoundSystem : EntitySystem
{
    [Dependency] private StationSystem _station = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary>
    /// Plays a non-admin global sound for players on the source entity's station
    /// and players who can see the source entity.
    /// </summary>
    /// <param name="source">Entity used to determine the station and PVS.</param>
    /// <param name="specifier">Resolved sound to play.</param>
    /// <param name="audioParams">Optional playback parameters.</param>
    [PublicAPI]
    public void PlayGlobalOnStation(EntityUid source, ResolvedSoundSpecifier specifier, AudioParams? audioParams = null)
    {
        var msg = new GameGlobalSoundEvent(specifier, audioParams);
        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    /// <summary>
    /// Requests clients in the source entity's station and PVS to stop station event music.
    /// </summary>
    /// <param name="source">Entity used to determine the station and PVS.</param>
    /// <param name="type">Category of station music to stop.</param>
    [PublicAPI]
    public void StopStationEventMusic(EntityUid source, StationEventMusicType type)
    {
        // TODO REPLAYS
        // these start & stop events are gonna be a PITA
        // theres probably some nice way of handling them. Maybe it just needs dedicated replay data (in which case these events should NOT get recorded).

        var msg = new StopStationEventMusic(type);
        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    /// <summary>
    /// Resolves and dispatches station event music at the default event-music volume.
    /// </summary>
    /// <param name="source">Entity used to determine the station and PVS.</param>
    /// <param name="sound">Sound to resolve and play.</param>
    /// <param name="type">Category used to track the music stream on clients.</param>
    [PublicAPI]
    public void DispatchStationEventMusic(EntityUid source, SoundSpecifier sound, StationEventMusicType type)
    {
        DispatchStationEventMusic(source, _audio.ResolveSound(sound), type);
    }

    /// <summary>
    /// Dispatches resolved station event music at the default event-music volume.
    /// </summary>
    /// <param name="source">Entity used to determine the station and PVS.</param>
    /// <param name="specifier">Resolved sound to play.</param>
    /// <param name="type">Category used to track the music stream on clients.</param>
    [PublicAPI]
    public void DispatchStationEventMusic(EntityUid source, ResolvedSoundSpecifier specifier, StationEventMusicType type)
    {
        var audio = AudioParams.Default.AddVolume(-8);
        var msg = new StationEventMusicEvent(specifier, type, audio);

        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    /// <summary>
    /// Builds a filter containing players on the source entity's station and players
    /// who can currently see the source entity.
    /// </summary>
    private Filter GetStationAndPvs(EntityUid source)
    {
        var stationFilter = _station.GetInOwningStation(source);
        stationFilter.AddPlayersByPvs(source, entityManager: EntityManager);
        return stationFilter;
    }
}

/// <summary>
/// Base network event carrying a sound and optional playback parameters.
/// </summary>
[Virtual]
[Serializable, NetSerializable]
public class GlobalSoundEvent(ResolvedSoundSpecifier specifier, AudioParams? audioParams = null)
    : EntityEventArgs
{
    public ResolvedSoundSpecifier Specifier = specifier;
    public AudioParams? AudioParams = audioParams;
}

/// <summary>
/// Intended for admin music. Can be disabled by the <seealso cref="CCVars.AdminSoundsEnabled"/> cvar.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminSoundEvent(ResolvedSoundSpecifier specifier, AudioParams? audioParams = null)
    : GlobalSoundEvent(specifier, audioParams);

/// <summary>
/// Intended for misc sound effects. Can't be disabled by cvar.
/// </summary>
[Serializable, NetSerializable]
public sealed class GameGlobalSoundEvent(ResolvedSoundSpecifier specifier, AudioParams? audioParams = null)
    : GlobalSoundEvent(specifier, audioParams);

/// <summary>
/// Identifies a category of station event music for start/stop matching.
/// </summary>
public enum StationEventMusicType : byte
{
    Nuke
}

/// <summary>
/// Intended for music triggered by events on a specific station. Can be disabled by the <seealso cref="CCVars.EventMusicEnabled"/> cvar.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationEventMusicEvent(
    ResolvedSoundSpecifier specifier,
    StationEventMusicType type,
    AudioParams? audioParams = null)
    : GlobalSoundEvent(specifier, audioParams)
{
    public StationEventMusicType Type = type;
}

/// <summary>
/// Attempts to stop a playing <seealso cref="StationEventMusicEvent"/> stream.
/// </summary>
[Serializable, NetSerializable]
public sealed class StopStationEventMusic(StationEventMusicType type) : EntityEventArgs
{
    public StationEventMusicType Type = type;
}
