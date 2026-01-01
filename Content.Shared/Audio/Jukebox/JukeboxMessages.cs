using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio.Jukebox;

[Serializable, NetSerializable]
public sealed class JukeboxPlayingMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxPauseMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxStopMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxRepeatMessage(bool repeat) : BoundUserInterfaceMessage
{
    public bool Repeat { get; } = repeat;
}

[Serializable, NetSerializable]
public sealed class JukeboxShuffleMessage(bool shuffle) : BoundUserInterfaceMessage
{
    public bool Shuffle { get; } = shuffle;
}

[Serializable, NetSerializable]
public sealed class JukeboxQueueTrackMessage(ProtoId<JukeboxPrototype> songId) : BoundUserInterfaceMessage
{
    public ProtoId<JukeboxPrototype> SongId { get; } = songId;
}

[Serializable, NetSerializable]
public sealed class JukeboxSetTimeMessage(float songTime) : BoundUserInterfaceMessage
{
    public float SongTime { get; } = songTime;
}

/// <summary>
///     Sent to the server to delete an item in the queue.
/// </summary>
[Serializable, NetSerializable]
public sealed class JukeboxDeleteRequestMessage(int index) : BoundUserInterfaceMessage
{
    public int Index = index;
}

/// <summary>
///     Sent to the server to move the position of an item in the queue.
/// </summary>
[Serializable, NetSerializable]
public sealed class JukeboxMoveRequestMessage(int index, int change) : BoundUserInterfaceMessage
{
    public int Index = index;
    public int Change = change;
}
