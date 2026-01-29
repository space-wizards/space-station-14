using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Audio.Events;

/// <summary>
/// Event of changing lobby music playlist (on server).
/// </summary>
[Serializable, NetSerializable]
public sealed class LobbyPlaylistChangedEvent : EntityEventArgs
{
    /// <inheritdoc />
    public LobbyPlaylistChangedEvent(ResPath[] playlist)
    {
        Playlist = playlist;
    }

    /// <summary>
    /// List of soundtrack filenames for lobby playlist.
    /// </summary>
    public ResPath[] Playlist;
}

/// <summary>
/// Event of stopping lobby music.
/// </summary>
[Serializable, NetSerializable]
public sealed class LobbyMusicStopEvent : EntityEventArgs
{
}
