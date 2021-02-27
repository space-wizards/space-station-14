using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Construction
{
    [Prototype("construction")]
    public class ConstructionPrototype : IPrototype
    {
        [DataField("conditions")]
        private List<IConstructionCondition> _conditions;

        /// <summary>
        ///     Friendly name displayed in the construction GUI.
        /// </summary>
        [DataField("name")]
        public string Name { get; private set; }

        /// <summary>
        ///     "Useful" description displayed in the construction GUI.
        /// </summary>
        [DataField("description")]
        public string Description { get; private set; }

        /// <summary>
        ///     The <see cref="ConstructionGraphPrototype"/> this construction will be using.
        /// </summary>
        [DataField("graph")]
        public string Graph { get; private set; }

        /// <summary>
        ///     The target <see cref="ConstructionGraphNode"/> this construction will guide the user to.
        /// </summary>
        [DataField("targetNode")]
        public string TargetNode { get; private set; }

        /// <summary>
        ///     The starting <see cref="ConstructionGraphNode"/> this construction will start at.
        /// </summary>
        [DataField("startNode")]
        public string StartNode { get; private set; }

        /// <summary>
        ///     Texture path inside the construction GUI.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

        /// <summary>
        ///     If you can start building or complete steps on impassable terrain.
        /// </summary>
        [DataField("canBuildInImpassable")]
        public bool CanBuildInImpassable { get; private set; }

        [DataField("category")]
        public string Category { get; private set; }

        [DataField("objectType")] public ConstructionType Type { get; private set; } = ConstructionType.Structure;

        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("parent")]
        public string Parent { get; }

        [DataField("placementMode")]
        public string PlacementMode { get; private set; } = "PlaceFree";

        /// <summary>
        ///     Whether this construction can be constructed rotated or not.
        /// </summary>
        [DataField("canRotate")]
        public bool CanRotate { get; private set; } = true;

        public IReadOnlyList<IConstructionCondition> Conditions => _conditions;
    }

    public enum ConstructionType
    {
        Structure,
        Item,
    }
}

