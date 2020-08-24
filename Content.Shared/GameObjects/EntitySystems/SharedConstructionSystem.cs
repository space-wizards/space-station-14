using System;
using Content.Shared.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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
            public readonly GridCoordinates Location;

            /// <summary>
            ///     The construction prototype to start building.
            /// </summary>
            public readonly string PrototypeName;

            public readonly Angle Angle;

            /// <summary>
            ///     Identifier to be sent back in the acknowledgement so that the client can clean up its ghost.
            /// </summary>
            public readonly int Ack;

            public TryStartStructureConstructionMessage(GridCoordinates loc, string prototypeName, Angle angle, int ack)
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

        public void DoExamine(FormattedMessage message, ConstructionPrototype prototype, int stage, bool inDetailRange)
        {
            var stages = prototype.Stages;
            if (stage >= 0 && stage < stages.Count)
            {
                var curStage = stages[stage];
                if (curStage.Backward != null && curStage.Backward is ConstructionStepTool)
                {
                    var backward = (ConstructionStepTool) curStage.Backward;
                    message.AddText(Loc.GetString("To deconstruct: {0}x {1} Tool", backward.Amount, backward.ToolQuality));
                }
                if (curStage.Forward != null && curStage.Forward is ConstructionStepMaterial)
                {
                    if (curStage.Backward != null)
                    {
                        message.AddText("\n");
                    }
                    var forward = (ConstructionStepMaterial) curStage.Forward;
                    message.AddText(Loc.GetString("To construct: {0}x {1}", forward.Amount, forward.Material));
                }
            }
        }
    }
}
