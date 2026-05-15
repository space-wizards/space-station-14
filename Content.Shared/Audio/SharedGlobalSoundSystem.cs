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
