using System;
using Robust.Shared.Serialization;

namespace Content.Shared.MusicPlayer;

[NetSerializable, Serializable]
public sealed class OpenMusicPlayerEvent : EntityEventArgs
{
}
