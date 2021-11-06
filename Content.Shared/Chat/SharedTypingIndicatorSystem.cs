using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            public NetUserId ClientId { get; }
            public EntityUid? EnityId { get; }

            public ClientTypingMessage(NetUserId id, EntityUid? owner)
            {
                ClientId = id;
                EnityId = owner;
            }
        }

        [Serializable, NetSerializable]
        public sealed class ClientStoppedTypingMessage : EntityEventArgs
        {
            public NetUserId ClientId { get; }
            public EntityUid? EnityId { get; }

            public ClientStoppedTypingMessage(NetUserId id, EntityUid? owner)
            {
                ClientId = id;
                EnityId = owner;
            }
        }

    }
}
