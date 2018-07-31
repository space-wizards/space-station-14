using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Map;
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
            public readonly GridLocalCoordinates Location;
            public readonly string PrototypeName;

            public TryStartStructureConstructionMessage(GridLocalCoordinates loc, string prototypeName)
            {
                Directed = true;
                Location = loc;
                PrototypeName = prototypeName;
            }
        }
    }
}
