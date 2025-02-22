using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
namespace Content.Shared.Audio;

/// <summary>
/// Handles playing audio to all players globally unless disabled by cvar. Some events are grid-specific.
/// </summary>
public abstract class SharedGlobalSoundSystem : EntitySystem
{
}

[Virtual]
[Serializable, NetSerializable]
public class GlobalSoundEvent : EntityEventArgs
{
    public string Filename;
    public AudioParams? AudioParams;
    public GlobalSoundEvent(string filename, AudioParams? audioParams = null)
    {
        Filename = filename;
        AudioParams = audioParams;
    }
}

/// <summary>
/// Intended for admin music. Can be disabled by the <seealso cref="CCVars.AdminSoundsEnabled"/> cvar.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminSoundEvent : GlobalSoundEvent
{
    public AdminSoundEvent(string filename, AudioParams? audioParams = null) : base(filename, audioParams){}
}

/// <summary>
/// Intended for misc sound effects. Can't be disabled by cvar.
/// </summary>
[Serializable, NetSerializable]
public sealed class GameGlobalSoundEvent : GlobalSoundEvent
{
    public GameGlobalSoundEvent(string filename, AudioParams? audioParams = null) : base(filename, audioParams){}
}

public enum StationEventMusicType : byte
{
    Nuke,
    CosmicCult, // Imp edit
}

/// <summary>
/// Intended for music triggered by events on a specific station. Can be disabled by the <seealso cref="CCVars.EventMusicEnabled"/> cvar.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationEventMusicEvent : GlobalSoundEvent
{
    public StationEventMusicType Type;

    public StationEventMusicEvent(string filename, StationEventMusicType type, AudioParams? audioParams = null) : base(
        filename, audioParams)
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
