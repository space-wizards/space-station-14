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

[Serializable, NetSerializable]
public sealed class AdminSoundEvent : GlobalSoundEvent
{
    public AdminSoundEvent(string filename, AudioParams? audioParams = null) : base(filename, audioParams){}
}

public enum StationEventMusicType : byte
{
    Nuke
}

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

[Serializable, NetSerializable]
public sealed class StopStationEventMusic : EntityEventArgs
{
    public StationEventMusicType Type;

    public StopStationEventMusic(StationEventMusicType type)
    {
        Type = type;
    }
}
