using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings
{
    [Prototype]
    public sealed partial class MarkingPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = "uwu";

        public string Name { get; private set; } = default!;

        [DataField("bodyPart", required: true)]
        public HumanoidVisualLayers BodyPart { get; private set; } = default!;

        [DataField]
        public List<ProtoId<MarkingsGroupPrototype>>? GroupWhitelist;

        [DataField("sexRestriction")]
        public Sex? SexRestriction { get; private set; }

        [DataField("forcedColoring")]
        public bool ForcedColoring { get; private set; } = false;

        [DataField("coloring")]
        public MarkingColors Coloring { get; private set; } = new();

        /// <summary>
        /// Do we need to apply any displacement maps to this marking? Set to false if your marking is incompatible
        /// with a standard human doll, and is used for some special races with unusual shapes
        /// </summary>
        [DataField]
        public bool CanBeDisplaced { get; private set; } = true;

        [DataField("sprites", required: true)]
        public List<SpriteSpecifier> Sprites { get; private set; } = default!;

        /// <summary>
        ///     Optional dictionary allowing assignment of shaders to sprite layers in a marking.
        ///     This implementation is very messy but unfortunately Robust doesn't like shaders in SpriteSpecifiers.
        /// </summary>
        [DataField]
        public Dictionary<string, string>? Shaders { get; private set; }

        public Marking AsMarking()
        {
            return new Marking(ID, Sprites.Count);
        }

        /// <summary>
        /// Chance this marking will be added by appearance randomizer.
        /// </summary>
        /// <remarks>
        /// Default value is 1.
        /// </remarks>
        [DataField]
        public float RandomWeight = 1f;
    }
}
