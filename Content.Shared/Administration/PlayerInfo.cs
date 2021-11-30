using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public record PlayerInfo(string Username, string CharacterName, bool Antag, EntityUid EntityUid, NetUserId SessionId);
}
