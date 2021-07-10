using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

#nullable enable

namespace Content.Shared.GameTicking
{
    [Serializable, NetSerializable]
    public class RoundRestartCleanupEvent : EntityEventArgs
    {
    }
}
