using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat
{
    public abstract class SharedTypingIndicatorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
        }

        [Serializable, NetSerializable]
        public sealed class ClientTypingMessage : EntityEventArgs
        {
            public NetUserId? ClientId { get; }
            public EntityUid? EnityId { get; }

            public ClientTypingMessage(NetUserId? id, EntityUid? owner)
            {
                ClientId = id;
                EnityId = owner;
            }
        }

    }
}
