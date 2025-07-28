using System.Linq;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : ICharacterAppearance, IEquatable<HumanoidCharacterAppearance>
{
    [DataField("hair")]
    public string HairStyleId { get; set; } = HairStyles.DefaultHairStyle;

    [DataField]
    public Color HairColor { get; set; } = Color.Black;
    [DataField]
    public bool HairGlowing { get; set; } = false; //starlight

    [DataField("facialHair")]
    public string FacialHairStyleId { get; set; } = HairStyles.DefaultFacialHairStyle;

    [DataField]
    public Color FacialHairColor { get; set; } = Color.Black;
    [DataField]
    public bool FacialHairGlowing { get; set; } = false; //starlight

    [DataField]
    public Color EyeColor { get; set; } = Color.Black;
    [DataField]
    public bool EyeGlowing { get; set; } = false; //starlight

    [DataField]
    public Color SkinColor { get; set; } = Humanoid.SkinColor.ValidHumanSkinTone;

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    public HumanoidCharacterAppearance(string hairStyleId,
        Color hairColor,
        bool hairGlowing, //starlight
        string facialHairStyleId,
        Color facialHairColor,
        bool facialHairGlowing, //starlight
        Color eyeColor,
        bool eyeGlowing, //starlight
        Color skinColor,
        List<Marking> markings)
    {
        HairStyleId = hairStyleId;
        HairColor = ClampColor(hairColor);
        FacialHairStyleId = facialHairStyleId;
        FacialHairColor = ClampColor(facialHairColor);
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        HairGlowing = hairGlowing; //starlight
        FacialHairGlowing = facialHairGlowing; //starlight
        EyeGlowing = eyeGlowing; //starlight
    }

    // starlight, function changed to support glowing
    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.HairStyleId, other.HairColor, other.HairGlowing, other.FacialHairStyleId, other.FacialHairColor, other.FacialHairGlowing, other.EyeColor, other.EyeGlowing, other.SkinColor, new(other.Markings))
    {

    }

    // starlight, function changed to support glowing
    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return new(newName, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings);
    }

    // starlight, function changed to support glowing
    public HumanoidCharacterAppearance WithHairColor(Color newColor)
    {
        return new(HairStyleId, newColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings);
    }

    // starlight start
    public HumanoidCharacterAppearance WithHairGlowing(bool newGlowing)
    {
        return new(HairStyleId, HairColor, newGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings);
    }
    // starlight end

    // starlight, function changed to support glowing
    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return new(HairStyleId, HairColor, HairGlowing, newName, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings);
    }

    // starlight start
    public HumanoidCharacterAppearance WithFacialHairGlowing(bool newGlowing)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, newGlowing, EyeColor, EyeGlowing, SkinColor, Markings);
    }
    // starlight end

    // starlight, function changed to support glowing
    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, newColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings);
    }

    // starlight, function changed to support glowing
    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, newColor, EyeGlowing, SkinColor, Markings);
    }

    // starlight start
    public HumanoidCharacterAppearance WithEyeGlowing(bool newGlowing)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, newGlowing, SkinColor, Markings);
    }
    // starlight end

    // starlight, function changed to support glowing
    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, newColor, Markings);
    }

    // starlight, function changed to support glowing
    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, newMarkings);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(string species)
    {
        var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);
        var skinColor = speciesPrototype.SkinColoration switch
        {
            HumanoidSkinColor.HumanToned => Humanoid.SkinColor.HumanSkinTone(speciesPrototype.DefaultHumanSkinTone),
            HumanoidSkinColor.Hues => speciesPrototype.DefaultSkinTone,
            HumanoidSkinColor.TintedHues => Humanoid.SkinColor.TintedHues(speciesPrototype.DefaultSkinTone),
            HumanoidSkinColor.VoxFeathers => Humanoid.SkinColor.ClosestVoxColor(speciesPrototype.DefaultSkinTone),
            _ => Humanoid.SkinColor.ValidHumanSkinTone,
        };

        return new(
            HairStyles.DefaultHairStyle,
            Color.Black,
            false, //starlight
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            false, //starlight
            Color.Black,
            false, //starlight
            skinColor,
            new ()
        );
    }

    private static IReadOnlyList<Color> RealisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, species).Keys.ToList();
        var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, species).Keys.ToList();

        var newHairStyle = hairStyles.Count > 0
            ? random.Pick(hairStyles)
            : HairStyles.DefaultHairStyle.Id;

        var newFacialHairStyle = facialHairStyles.Count == 0 || sex == Sex.Female
            ? HairStyles.DefaultFacialHairStyle.Id
            : random.Pick(facialHairStyles);

        var newHairColor = random.Pick(HairStyles.RealisticHairColors);
        newHairColor = newHairColor
            .WithRed(RandomizeColor(newHairColor.R))
            .WithGreen(RandomizeColor(newHairColor.G))
            .WithBlue(RandomizeColor(newHairColor.B));

        // TODO: Add random markings

        var eyeType = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species).EyeColoration; // Starlight

        var newEyeColor = random.Pick(RealisticEyeColors);

        // Starlight - Start
        switch (eyeType)
        {
            case HumanoidEyeColor.Shadekin:
                newEyeColor = Humanoid.EyeColor.MakeShadekinValid(newEyeColor);
                break;
            default:
                break;
                
        }
        // Starlight - End

        var skinType = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species).SkinColoration;

        var newSkinColor = new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1);
        switch (skinType)
        {
            case HumanoidSkinColor.HumanToned:
                newSkinColor = Humanoid.SkinColor.HumanSkinTone(random.Next(0, 101));
                break;
            case HumanoidSkinColor.Hues:
                break;
            case HumanoidSkinColor.TintedHues:
                newSkinColor = Humanoid.SkinColor.ValidTintedHuesSkinTone(newSkinColor);
                break;
            case HumanoidSkinColor.VoxFeathers:
                newSkinColor = Humanoid.SkinColor.ProportionalVoxColor(newSkinColor);
                break;
        }

        return new HumanoidCharacterAppearance(newHairStyle, newHairColor, false, newFacialHairStyle, newHairColor, false, newEyeColor, false, newSkinColor, new ()); //starlight, glowing

        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, string species, Sex sex)
    {
        var hairStyleId = appearance.HairStyleId;
        var facialHairStyleId = appearance.FacialHairStyleId;

        var hairColor = ClampColor(appearance.HairColor);
        var facialHairColor = ClampColor(appearance.FacialHairColor);
        var eyeColor = ClampColor(appearance.EyeColor);

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        if (!markingManager.MarkingsByCategory(MarkingCategories.Hair).ContainsKey(hairStyleId))
        {
            hairStyleId = HairStyles.DefaultHairStyle;
        }

        if (!markingManager.MarkingsByCategory(MarkingCategories.FacialHair).ContainsKey(facialHairStyleId))
        {
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }

        var markingSet = new MarkingSet();
        var skinColor = appearance.SkinColor;
        if (proto.TryIndex(species, out SpeciesPrototype? speciesProto))
        {
            markingSet = new MarkingSet(appearance.Markings, speciesProto.MarkingPoints, markingManager, proto);
            markingSet.EnsureValid(markingManager);

            if (!Humanoid.SkinColor.VerifySkinColor(speciesProto.SkinColoration, skinColor))
            {
                skinColor = Humanoid.SkinColor.ValidSkinTone(speciesProto.SkinColoration, skinColor);
            }

            // Starlight - Start
            if (!Humanoid.EyeColor.VerifyEyeColor(speciesProto.EyeColoration, eyeColor))
            {
                eyeColor = Humanoid.EyeColor.ValidEyeColor(speciesProto.EyeColoration, eyeColor);
            }
            // Starlight - End

            markingSet.EnsureSpecies(species, skinColor, markingManager);
            markingSet.EnsureSexes(sex, markingManager);
        }

        return new HumanoidCharacterAppearance(
            hairStyleId,
            hairColor,
            appearance.HairGlowing, //starlight
            facialHairStyleId,
            facialHairColor,
            appearance.FacialHairGlowing, //starlight
            eyeColor,
            appearance.EyeGlowing, //starlight
            skinColor,
            markingSet.GetForwardEnumerator().ToList());
    }

    public bool MemberwiseEquals(ICharacterAppearance maybeOther)
    {
        if (maybeOther is not HumanoidCharacterAppearance other) return false;
        if (HairStyleId != other.HairStyleId) return false;
        if (!HairColor.Equals(other.HairColor)) return false;
        if (HairGlowing != other.HairGlowing) return false; //starlight
        if (FacialHairStyleId != other.FacialHairStyleId) return false;
        if (!FacialHairColor.Equals(other.FacialHairColor)) return false;
        if (FacialHairGlowing != other.FacialHairGlowing) return false; //starlight
        if (!EyeColor.Equals(other.EyeColor)) return false;
        if (EyeGlowing != other.EyeGlowing) return false; //starlight
        if (!SkinColor.Equals(other.SkinColor)) return false;
        if (!Markings.SequenceEqual(other.Markings)) return false;
        return true;
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return HairStyleId == other.HairStyleId &&
               HairColor.Equals(other.HairColor) &&
               HairGlowing.Equals(other.HairGlowing) && //starlight
               FacialHairStyleId == other.FacialHairStyleId &&
               FacialHairColor.Equals(other.FacialHairColor) &&
               FacialHairGlowing.Equals(other.FacialHairGlowing) && //starlight
               EyeColor.Equals(other.EyeColor) &&
               EyeGlowing.Equals(other.EyeGlowing) && //starlight
               SkinColor.Equals(other.SkinColor) &&
               Markings.SequenceEqual(other.Markings);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        //starlight, reworked this function as it exceeded the original limit for the built in hashcode generics
        //now it uses the add syntax to add each field to the hashcode
        HashCode hash = new();
        hash.Add(HairStyleId);
        hash.Add(HairColor);
        hash.Add(HairGlowing);
        hash.Add(FacialHairStyleId);
        hash.Add(FacialHairColor);
        hash.Add(FacialHairGlowing);
        hash.Add(EyeColor);
        hash.Add(EyeGlowing);
        hash.Add(SkinColor);
        hash.Add(Markings);
        return hash.ToHashCode();
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
