#nullable enable
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
        [DataField("conditions")] private List<IConstructionCondition> _conditions = new();

        /// <summary>
        ///     Friendly name displayed in the construction GUI.
        /// </summary>
        [DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        ///     "Useful" description displayed in the construction GUI.
        /// </summary>
        [DataField("description")]
        public string Description { get; } = string.Empty;

        /// <summary>
        ///     The <see cref="ConstructionGraphPrototype"/> this construction will be using.
        /// </summary>
        [DataField("graph")]
        public string Graph { get; } = string.Empty;

        /// <summary>
        ///     The target <see cref="ConstructionGraphNode"/> this construction will guide the user to.
        /// </summary>
        [DataField("targetNode")]
        public string TargetNode { get; } = string.Empty;

        /// <summary>
        ///     The starting <see cref="ConstructionGraphNode"/> this construction will start at.
        /// </summary>
        [DataField("startNode")]
        public string StartNode { get; } = string.Empty;

        /// <summary>
        ///     Texture path inside the construction GUI.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier Icon { get; } = SpriteSpecifier.Invalid;

        /// <summary>
        ///     If you can start building or complete steps on impassable terrain.
        /// </summary>
        [DataField("canBuildInImpassable")]
        public bool CanBuildInImpassable { get; private set; }

        [DataField("category")] public string Category { get; private set; } = string.Empty;

        [DataField("objectType")] public ConstructionType Type { get; private set; } = ConstructionType.Structure;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("placementMode")]
        public string PlacementMode { get; } = "PlaceFree";

        /// <summary>
        ///     Whether this construction can be constructed rotated or not.
        /// </summary>
        [DataField("canRotate")]
        public bool CanRotate { get; } = true;

        public IReadOnlyList<IConstructionCondition> Conditions => _conditions;
    }

    public enum ConstructionType
    {
        Structure,
        Item,
    }
}

