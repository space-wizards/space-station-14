using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Construction
{
    /// <summary>
    ///     Basically handles the logic of "this mob can do construction".
    /// </summary>
    public abstract class SharedConstructorComponent : Component
    {
        public override string Name => "Constructor";
        public override uint? NetID => ContentNetIDs.CONSTRUCTOR;

        /// <summary>
        ///     Sent client -> server to to tell the server that we started building
        ///     a structure-construction.
        /// </summary>
        [Serializable, NetSerializable]
        protected class TryStartStructureConstructionMessage : ComponentMessage
        {
            /// <summary>
            ///     Position to start building.
            /// </summary>
            public readonly GridLocalCoordinates Location;

            /// <summary>
            ///     The construction prototype to start building.
            /// </summary>
            public readonly string PrototypeName;

            public readonly Angle Angle;

            /// <summary>
            ///     Identifier to be sent back in the acknowledgement so that the client can clean up its ghost.
            /// </summary>
            public readonly int Ack;

            public TryStartStructureConstructionMessage(GridLocalCoordinates loc, string prototypeName, Angle angle, int ack)
            {
                Directed = true;
                Location = loc;
                PrototypeName = prototypeName;
                Angle = angle;
                Ack = ack;
            }
        }

        [Serializable, NetSerializable]
        protected class AckStructureConstructionMessage : ComponentMessage
        {
            public readonly int Ack;

            public AckStructureConstructionMessage(int ack)
            {
                Directed = true;
                Ack = ack;
            }
        }
    }
}
