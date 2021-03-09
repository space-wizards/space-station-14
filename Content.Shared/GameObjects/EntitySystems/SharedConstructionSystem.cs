#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class SharedConstructionSystem : EntitySystem
    {
        /// <summary>
        ///     Sent client -> server to to tell the server that we started building
        ///     a structure-construction.
        /// </summary>
        [Serializable, NetSerializable]
        public class TryStartStructureConstructionMessage : EntitySystemMessage
        {
            /// <summary>
            ///     Position to start building.
            /// </summary>
            public readonly EntityCoordinates Location;

            /// <summary>
            ///     The construction prototype to start building.
            /// </summary>
            public readonly string PrototypeName;

            public readonly Angle Angle;

            /// <summary>
            ///     Identifier to be sent back in the acknowledgement so that the client can clean up its ghost.
            /// </summary>
            public readonly int Ack;

            public TryStartStructureConstructionMessage(EntityCoordinates loc, string prototypeName, Angle angle, int ack)
            {
                Location = loc;
                PrototypeName = prototypeName;
                Angle = angle;
                Ack = ack;
            }
        }

        /// <summary>
        ///     Sent client -> server to to tell the server that we started building
        ///     an item-construction.
        /// </summary>
        [Serializable, NetSerializable]
        public class TryStartItemConstructionMessage : EntitySystemMessage
        {
            /// <summary>
            ///     The construction prototype to start building.
            /// </summary>
            public readonly string PrototypeName;

            public TryStartItemConstructionMessage(string prototypeName)
            {
                PrototypeName = prototypeName;
            }
        }

        /// <summary>
        /// Send server -> client to tell the client that a ghost has started to be constructed.
        /// </summary>
        [Serializable, NetSerializable]
        public class AckStructureConstructionMessage : EntitySystemMessage
        {
            public readonly int GhostId;

            public AckStructureConstructionMessage(int ghostId)
            {
                GhostId = ghostId;
            }
        }
    }
}
