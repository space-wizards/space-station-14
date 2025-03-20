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
    public List<string>? SpeciesRestriction { get; private set; }

    [DataField]
    public Sex? SexRestriction { get; private set; }

    [DataField]
    public bool FollowSkinColor { get; private set; }

    [DataField]
    public bool ForcedColoring { get; private set; }

    [DataField]
    public MarkingColors Coloring { get; private set; } = new();

    /// <summary>
    /// Do we need to apply any displacement maps to this marking? Set to false if your marking is incompatible
    /// with a standard human doll, and is used for some special races with unusual shapes
    /// </summary>
    [DataField]
    public bool SupportDisplacement { get; private set; } = true;

    [DataField(required: true)]
    public List<SpriteSpecifier> Sprites { get; private set; } = default!;

    public Marking AsMarking()
    {
        return new Marking(ID, Sprites.Count);
    }
}
