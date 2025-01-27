using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings
{
    [Prototype("marking")]
    public sealed partial class MarkingPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = "uwu";

        public string Name { get; private set; } = default!;

        [DataField("bodyPart", required: true)]
        public HumanoidVisualLayers BodyPart { get; private set; } = default!;

        [DataField("markingCategory", required: true)]
        public MarkingCategories MarkingCategory { get; private set; } = default!;

        [DataField("speciesRestriction")]
        public List<string>? SpeciesRestrictions { get; private set; }

        [DataField("sexRestriction")]
        public Sex? SexRestriction { get; private set; }

        [DataField("followSkinColor")]
        public bool FollowSkinColor { get; private set; } = false;

        [DataField("forcedColoring")]
        public bool ForcedColoring { get; private set; } = false;

        [DataField("coloring")]
        public MarkingColors Coloring { get; private set; } = new();

        [DataField("sprites", required: true)]
        public List<SpriteSpecifier> Sprites { get; private set; } = default!;
		
		[DataField("shader")]
		public string? Shader { get; private set; } = null;
		
        public Marking AsMarking()
        {
            return new Marking(ID, Sprites.Count);
        }
    }
}
