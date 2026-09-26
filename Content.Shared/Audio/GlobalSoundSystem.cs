using Content.Shared.CCVar;
using Content.Shared.Station.Systems;
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

    private Filter GetStationAndPvs(EntityUid source)
    {
        var stationFilter = _station.GetInOwningStation(source);
        stationFilter.AddPlayersByPvs(source, entityManager: EntityManager);
        return stationFilter;
    }

    public void PlayGlobalOnStation(EntityUid source, ResolvedSoundSpecifier specifier, AudioParams? audioParams = null)
    {
        var msg = new GameGlobalSoundEvent(specifier, audioParams);
        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    public void StopStationEventMusic(EntityUid source, StationEventMusicType type)
    {
        // TODO REPLAYS
        // these start & stop events are gonna be a PITA
        // theres probably some nice way of handling them. Maybe it just needs dedicated replay data (in which case these events should NOT get recorded).

        var msg = new StopStationEventMusic(type);
        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    public void DispatchStationEventMusic(EntityUid source, SoundSpecifier sound, StationEventMusicType type)
    {
        DispatchStationEventMusic(source, _audio.ResolveSound(sound), type);
    }

    public void DispatchStationEventMusic(EntityUid source, ResolvedSoundSpecifier specifier, StationEventMusicType type)
    {
        var audio = AudioParams.Default.AddVolume(-8);
        var msg = new StationEventMusicEvent(specifier, type, audio);

        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }
}

[Virtual]
[Serializable, NetSerializable]
public class GlobalSoundEvent : EntityEventArgs
{
    public ResolvedSoundSpecifier Specifier;
    public AudioParams? AudioParams;
    public GlobalSoundEvent(ResolvedSoundSpecifier specifier, AudioParams? audioParams = null)
    {
        Specifier = specifier;
        AudioParams = audioParams;
    }
}

/// <summary>
/// Intended for admin music. Can be disabled by the <seealso cref="CCVars.AdminSoundsEnabled"/> cvar.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminSoundEvent : GlobalSoundEvent
{
    public AdminSoundEvent(ResolvedSoundSpecifier specifier, AudioParams? audioParams = null) : base(specifier, audioParams){}
}

/// <summary>
/// Intended for misc sound effects. Can't be disabled by cvar.
/// </summary>
[Serializable, NetSerializable]
public sealed class GameGlobalSoundEvent : GlobalSoundEvent
{
    public GameGlobalSoundEvent(ResolvedSoundSpecifier specifier, AudioParams? audioParams = null) : base(specifier, audioParams){}
}

public enum StationEventMusicType : byte
{
    Nuke
}

/// <summary>
/// Intended for music triggered by events on a specific station. Can be disabled by the <seealso cref="CCVars.EventMusicEnabled"/> cvar.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationEventMusicEvent : GlobalSoundEvent
{
    public StationEventMusicType Type;

    public StationEventMusicEvent(ResolvedSoundSpecifier specifier, StationEventMusicType type, AudioParams? audioParams = null) : base(
        specifier, audioParams)
    {
        Type = type;
    }
}

/// <summary>
/// Attempts to stop a playing <seealso cref="StationEventMusicEvent"/> stream.
/// </summary>
[Serializable, NetSerializable]
public sealed class StopStationEventMusic : EntityEventArgs
{
    public StationEventMusicType Type;

    public StopStationEventMusic(StationEventMusicType type)
    {
        Type = type;
    }
}
