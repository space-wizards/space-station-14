using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings
{
    [Prototype("marking")]
    public sealed class MarkingPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = "uwu";

        public string Name { get; private set; } = default!;

        [DataField("bodyPart", required: true)]
        public HumanoidVisualLayers BodyPart { get; } = default!;

        [DataField("markingCategory", required: true)]
        public MarkingCategories MarkingCategory { get; } = default!;

        [DataField("speciesRestriction")]
        public List<string>? SpeciesRestrictions { get; }

        // Corvax-Sponsors-Start
        [DataField("sponsorOnly")]
        public bool SponsorOnly = false;
        // Corvax-Sponsors-End

        [DataField("followSkinColor")]
        public bool FollowSkinColor { get; } = false;

        [DataField("sprites", required: true)]
        public List<SpriteSpecifier> Sprites { get; private set; } = default!;

        public Marking AsMarking()
        {
            return new Marking(ID, Sprites.Count);
        }
    }
}
