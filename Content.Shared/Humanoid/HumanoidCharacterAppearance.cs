using System.Linq;
using System.Numerics;
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
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    [DataField]
    public float Width { get; set; } = 1f; //starlight

    [DataField]
    public float Height { get; set; } = 1f; //starlight

    public HumanoidCharacterAppearance(string hairStyleId,
        Color hairColor,
        bool hairGlowing, //starlight
        string facialHairStyleId,
        Color facialHairColor,
        bool facialHairGlowing, //starlight
        Color eyeColor,
        bool eyeGlowing, //starlight
        Color skinColor,
        List<Marking> markings,
        float width, //starlight
        float height) //starlight
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
        Width = width; //starlight
        Height = height; //starlight
    }

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.HairStyleId, other.HairColor, other.HairGlowing, other.FacialHairStyleId, other.FacialHairColor, other.FacialHairGlowing, other.EyeColor, other.EyeGlowing, other.SkinColor, new(other.Markings), other.Width, other.Height)
    {

    }

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return new(newName, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings, Width, Height);
    }

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance WithHairColor(Color newColor)
    {
        return new(HairStyleId, newColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings, Width, Height);
    }

    // starlight start
    public HumanoidCharacterAppearance WithHairGlowing(bool newGlowing)
    {
        return new(HairStyleId, HairColor, newGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings, Width, Height);
    }
    // starlight end

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return new(HairStyleId, HairColor, HairGlowing, newName, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings, Width, Height);
    }

    // starlight start
    public HumanoidCharacterAppearance WithFacialHairGlowing(bool newGlowing)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, newGlowing, EyeColor, EyeGlowing, SkinColor, Markings, Width, Height);
    }
    // starlight end

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, newColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings, Width, Height);
    }

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, newColor, EyeGlowing, SkinColor, Markings, Width, Height);
    }

    // starlight start
    public HumanoidCharacterAppearance WithEyeGlowing(bool newGlowing)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, newGlowing, SkinColor, Markings, Width, Height);
    }
    // starlight end

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, newColor, Markings, Width, Height);
    }

    // starlight, function changed to support glowing, size
    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, newMarkings, Width, Height);
    }

    //starlight start
    public HumanoidCharacterAppearance WithWidth(float newWidth)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings, newWidth, Height);
    }

    public HumanoidCharacterAppearance WithHeight(float newHeight)
    {
        return new(HairStyleId, HairColor, HairGlowing, FacialHairStyleId, FacialHairColor, FacialHairGlowing, EyeColor, EyeGlowing, SkinColor, Markings, Width, newHeight);
    }
    //starlight end

    public static HumanoidCharacterAppearance DefaultWithSpecies(string species)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);
        var skinColoration = protoMan.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            SkinColorationStrategyInput.Color => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
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
            new (),
            speciesPrototype.DefaultWidth, //starlight
            speciesPrototype.DefaultHeight //starlight
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

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var skinType = protoMan.Index<SpeciesPrototype>(species).SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;
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

        var newSkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };

        //starlight start
        var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);
        var newWidth = random.NextFloat(speciesPrototype.MinWidth, speciesPrototype.MaxWidth);
        var newHeight = random.NextFloat(speciesPrototype.MinHeight, speciesPrototype.MaxHeight);
        //starlight end

        return new HumanoidCharacterAppearance(newHairStyle, newHairColor, false, newFacialHairStyle, newHairColor, false, newEyeColor, false, newSkinColor, new (), newWidth, newHeight); //starlight, glowing

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

        var width = appearance.Width; //starlight
        var height = appearance.Height; //starlight

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

            var strategy = proto.Index(speciesProto.SkinColoration).Strategy;
            skinColor = strategy.EnsureVerified(skinColor);

            // Starlight - Start
            if (!Humanoid.EyeColor.VerifyEyeColor(speciesProto.EyeColoration, eyeColor))
            {
                eyeColor = Humanoid.EyeColor.ValidEyeColor(speciesProto.EyeColoration, eyeColor);
            }

            // this isn't a clamp, it's a reset if either is out of range
            // maximum is done so that small species will get the correct height if they are defaulted (1f dwarf becoming 0.8f for example)
            // minimum is done so that null values (interpreted as 0f) will get the default height and not become miniatures
            if (width > speciesProto.MaxWidth || width < speciesProto.MinWidth) width = speciesProto.DefaultWidth;
            if (height > speciesProto.MaxHeight || height < speciesProto.MinHeight) height = speciesProto.DefaultHeight;
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
            markingSet.GetForwardEnumerator().ToList(),
            width, //starlight
            height); //starlight
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
        if (Width != other.Width) return false; //starlight
        if (Height != other.Height) return false; //starlight
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
               Markings.SequenceEqual(other.Markings) &&
               Width == other.Width && //starlight
               Height == other.Height; //starlight
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
        hash.Add(EyeGlowing); //starlight
        hash.Add(SkinColor);
        hash.Add(Markings);
        hash.Add(Width); //starlight
        hash.Add(Height); //starlight
        return hash.ToHashCode();
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
