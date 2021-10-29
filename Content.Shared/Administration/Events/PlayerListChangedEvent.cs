using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events
{
    [Serializable, NetSerializable]
    public class PlayerListChangedEvent : EntityEventArgs
    {
        public List<PlayerInfo> PlayersInfo = new();

        [Serializable, NetSerializable]
        public record PlayerInfo(string Username, string CharacterName, bool Antag, EntityUid EntityUid, ICommonSession Session);
    }
}
