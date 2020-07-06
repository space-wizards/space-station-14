using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    /// <summary>
    /// Holds data about an entity that is in the process of being constructed or destructed.
    /// </summary>
    [RegisterComponent]
    public class ConstructionComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "Construction";

        /// <summary>
        /// The current construction recipe being used to build this entity.
        /// </summary>
        [ViewVariables]
        public ConstructionPrototype Prototype { get; set; }

        /// <summary>
        /// The current stage of construction.
        /// </summary>
        [ViewVariables]
        public int Stage { get; set; }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("prototype", null,
                value => Prototype = value, () => Prototype);

            serializer.DataReadWriteFunction("stage", 0,
                value => Stage = value, () => Stage);
        }
    }
}
