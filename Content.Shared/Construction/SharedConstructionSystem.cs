using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public class SharedConstructionSystem : EntitySystem
    {
        /// <summary>
        ///     Sent client -> server to to tell the server that we started building
        ///     a structure-construction.
        /// </summary>
        [Serializable, NetSerializable]
        public class TryStartStructureConstructionMessage : EntityEventArgs
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
        public class TryStartItemConstructionMessage : EntityEventArgs
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
        /// Sent server -> client to tell the client that a ghost has started to be constructed.
        /// </summary>
        [Serializable, NetSerializable]
        public class AckStructureConstructionMessage : EntityEventArgs
        {
            public readonly int GhostId;

            public AckStructureConstructionMessage(int ghostId)
            {
                GhostId = ghostId;
            }
        }

        /// <summary>
        /// Sent client -> server to request a specific construction guide.
        /// </summary>
        [Serializable, NetSerializable]
        public class RequestConstructionGuide : EntityEventArgs
        {
            public readonly string ConstructionId;

            public RequestConstructionGuide(string constructionId)
            {
                ConstructionId = constructionId;
            }
        }

        /// <summary>
        /// Sent server -> client as a response to a <see cref="RequestConstructionGuide"/> net message.
        /// </summary>
        [Serializable, NetSerializable]
        public class ResponseConstructionGuide : EntityEventArgs
        {
            public readonly string ConstructionId;
            public readonly ConstructionGuide Guide;

            public ResponseConstructionGuide(string constructionId, ConstructionGuide guide)
            {
                ConstructionId = constructionId;
                Guide = guide;
            }
        }
    }
}
