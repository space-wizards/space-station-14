using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

[Prototype]
public sealed partial class MarkingPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = "uwu"; //uwu

    public string Name { get; private set; } = default!;

    [DataField(required: true)]
    public HumanoidVisualLayers BodyPart { get; private set; }

    [DataField(required: true)]
    public MarkingCategories MarkingCategory { get; private set; }

    [DataField]
    public List<string>? SpeciesRestrictions { get; private set; }

    [DataField]
    public Sex? SexRestriction { get; private set; }

    [DataField]
    public bool FollowSkinColor { get; private set; }

    [DataField]
    public bool ForcedColoring { get; private set; }

    [DataField]
    public MarkingColors Coloring { get; private set; } = new();

    [DataField(required: true)]
    public List<SpriteSpecifier> Sprites { get; private set; } = default!;

    public Marking AsMarking()
    {
        return new Marking(ID, Sprites.Count);
    }
}
