using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    [Prototype("construction")]
    public class ConstructionPrototype : IPrototype, IIndexedPrototype
    {
        [YamlField("conditions")]
        private List<IConstructionCondition> _conditions;

        /// <summary>
        ///     Friendly name displayed in the construction GUI.
        /// </summary>
        [YamlField("name")]
        public string Name { get; private set; }

        /// <summary>
        ///     "Useful" description displayed in the construction GUI.
        /// </summary>
        [YamlField("description")]
        public string Description { get; private set; }

        /// <summary>
        ///     The <see cref="ConstructionGraphPrototype"/> this construction will be using.
        /// </summary>
        [YamlField("graph")]
        public string Graph { get; private set; }

        /// <summary>
        ///     The target <see cref="ConstructionGraphNode"/> this construction will guide the user to.
        /// </summary>
        [YamlField("targetNode")]
        public string TargetNode { get; private set; }

        /// <summary>
        ///     The starting <see cref="ConstructionGraphNode"/> this construction will start at.
        /// </summary>
        [YamlField("startNode")]
        public string StartNode { get; private set; }

        /// <summary>
        ///     Texture path inside the construction GUI.
        /// </summary>
        [YamlField("icon")]
        public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

        /// <summary>
        ///     If you can start building or complete steps on impassable terrain.
        /// </summary>
        [YamlField("canBuildInImpassable")]
        public bool CanBuildInImpassable { get; private set; }

        [YamlField("category")]
        public string Category { get; private set; }

        [YamlField("objectType")] public ConstructionType Type { get; private set; } = ConstructionType.Structure;

        [YamlField("id")]
        public string ID { get; private set; }

        [YamlField("placementMode")]
        public string PlacementMode { get; private set; } = "PlaceFree";

        /// <summary>
        ///     Whether this construction can be constructed rotated or not.
        /// </summary>
        [YamlField("canRotate")]
        public bool CanRotate { get; private set; } = true;

        public IReadOnlyList<IConstructionCondition> Conditions => _conditions;
    }

    public enum ConstructionType
    {
        Structure,
        Item,
    }
}

