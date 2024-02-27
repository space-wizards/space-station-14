using Robust.Shared.Serialization;

namespace Content.Shared.Audio.Events;

[Serializable, NetSerializable]
public sealed class LobbyPlaylistChangedEvent : EntityEventArgs
{
    /// <inheritdoc />
    public LobbyPlaylistChangedEvent(string[] playlist)
    {
        Playlist = playlist;
    }

    public string[] Playlist;
}

[Serializable, NetSerializable]
public sealed class LobbySongStoppedEvent : EntityEventArgs
{
}
