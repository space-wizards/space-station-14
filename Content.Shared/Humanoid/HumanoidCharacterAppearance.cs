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

    [DataField("facialHair")]
    public string FacialHairStyleId { get; set; } = HairStyles.DefaultFacialHairStyle;

    [DataField]
    public Color FacialHairColor { get; set; } = Color.Black;

    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Humanoid.SkinColor.ValidHumanSkinTone;

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    public HumanoidCharacterAppearance(string hairStyleId,
        Color hairColor,
        string facialHairStyleId,
        Color facialHairColor,
        Color eyeColor,
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
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.HairStyleId, other.HairColor, other.FacialHairStyleId, other.FacialHairColor, other.EyeColor, other.SkinColor, new(other.Markings))
    {

    }

    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return new(newName, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance WithHairColor(Color newColor)
    {
        return new(HairStyleId, newColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return new(HairStyleId, HairColor, newName, FacialHairColor, EyeColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, newColor, EyeColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, newColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, newColor, Markings);
    }

    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, newMarkings);
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
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            Color.Black,
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

        var newFacialHairStyle = HairStyles.DefaultFacialHairStyle;
        var newHairStyle = HairStyles.DefaultHairStyle;
        List<Marking> newMarkings = [];

        // grab a completely random color.
        var baseColor = new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1);

        // create a new color palette based on BaseColor. roll to determine what type of palette it is.
        // personally I think this should be weighted, but I can't be bothered to implement that.
        List<Color> colorPalette = [];
        switch (random.Next(3))
        {
            case 0:
                colorPalette = GetSplitComplementaries(baseColor);
                break;
            case 1:
                colorPalette = GetTriadicComplementaries(baseColor);
                break;
            case 2:
                colorPalette = GetOneComplementary(baseColor);
                break;
        }

        // grab the species skin coloration type.
        var skinType = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species).SkinColoration;

        // declare some defaults. ensures that the hair and eyes on hues-colored species don't match the skin or one another.
        var newSkinColor = colorPalette[0];
        var newHairColor = colorPalette[1];
        var newEyeColor = colorPalette[2];

        // now we do some color logic.
        switch (skinType)
        {
            // if the species is HumanToned:
            case HumanoidSkinColor.HumanToned:
                // quantize the randomized skin color to the nearest acceptable HumanToned color.
                var tone = Math.Round(Humanoid.SkinColor.HumanSkinToneFromColor(newSkinColor));
                newSkinColor = Humanoid.SkinColor.HumanSkinTone((int)tone);

                // pick a random realistic hair color from the list and randomize it juuuuust a little bit.
                newHairColor = random.Pick(HairStyles.RealisticHairColors);
                newHairColor = newHairColor
                    .WithRed(RandomizeColor(newHairColor.R))
                    .WithGreen(RandomizeColor(newHairColor.G))
                    .WithBlue(RandomizeColor(newHairColor.B));

                // and pick a random realistic eye color from the list.
                newEyeColor = random.Pick(RealisticEyeColors);

                // we're also going to crush the other colors down to the skin's luminosity so markings don't appear too bright on darker skin.
                colorPalette[1] = SquashToSkinLuminosity(newSkinColor, colorPalette[1]);
                colorPalette[2] = SquashToSkinLuminosity(newSkinColor, colorPalette[2]);
                break;

            // if the species is Hues toned: it's fine the way it is.
            case HumanoidSkinColor.Hues:
                break;

            // if the species is TintedHues toned: tint them hues.
            case HumanoidSkinColor.TintedHues:
                newSkinColor = Humanoid.SkinColor.ValidTintedHuesSkinTone(newSkinColor);

                // we're also going to crush the other colors down to valid TintedHues skin colors.
                colorPalette[1] = Humanoid.SkinColor.ValidTintedHuesSkinTone(colorPalette[1]);
                colorPalette[2] = Humanoid.SkinColor.ValidTintedHuesSkinTone(colorPalette[2]);
                break;

            // if the species is VoxFeathers toned: confine the skin color to vox limits. Bright colors are otherwise fine, so leave the marking colors alone.
            case HumanoidSkinColor.VoxFeathers:
                newSkinColor = Humanoid.SkinColor.ProportionalVoxColor(newSkinColor);
                break;
        }

        // now we loop through every extant marking category,
        foreach (var category in Enum.GetValues<MarkingCategories>())
        {
            // grab a list of markings in that category for that species,
            var markings = markingManager.MarkingsByCategoryAndSpecies(category, species).Keys.ToList();
            var markingProtos = markingManager.MarkingsByCategoryAndSpecies(category, species).Values.ToList();

            // if it's facial hair, there are entries in the category, and the character is not female, assign a random one. else bald
            if (category == MarkingCategories.FacialHair)
            {
                newFacialHairStyle = markings.Count == 0 || sex == Sex.Female ? HairStyles.DefaultFacialHairStyle : random.Pick(markings);
            }

            // if it's hair, and there are hair styles, roll one. else bald
            else if (category == MarkingCategories.Hair)
            {
                newHairStyle = markings.Count > 0 ? random.Pick(markings) : HairStyles.DefaultHairStyle;
            }

            // for every other category,
            else if (markings.Count > 0)
            {
                // roll a die. currently a 1 in 3 chance per category, except Tails, which are 1 in 2 (because of the effect they have on the silhouettes of spiders and moths.)
                int diceRoll;
                if (category == MarkingCategories.Tail)
                    diceRoll = random.Next(2);
                else
                    diceRoll = random.Next(3);

                if (diceRoll == 0)
                {
                    MarkingPrototype? lastMarking = null;

                    // roll to see how many markings from that category will be added. currently a maximum of 2.
                    var loops = random.Next(2) + 1;

                    // add a marking (loops) times
                    for (var i = 0; i < loops; i++)
                    {
                        // pick a random marking from the list
                        var protoToAdd = random.Pick(markingProtos);
                        var markingToAdd = protoToAdd.AsMarking();
                        Color markingColor;

                        // prevent duplicates:
                        if (lastMarking != null && lastMarking == protoToAdd)
                            continue;

                        // set gauze to white.
                        // side note, I really hate that gauze isn't its own category. please fix that so that i can make this not suck as much.
                        // or, like, give it its own color rules. or something.
                        if (markingToAdd.MarkingId.Contains("gauze", StringComparison.OrdinalIgnoreCase))
                        {
                            markingToAdd.SetColor(Color.White);
                            newMarkings.Add(markingToAdd);
                            lastMarking = protoToAdd;
                            continue;
                        }

                        // select a random color from our two secondary colors. if our marking is a Tail, add the skin color as well, otherwise lizards always look a little odd.
                        // this will also make moths and spiders look less interesting on average, but I don't want a hardcoded exception for lizards.
                        if (category == MarkingCategories.Tail)
                            markingColor = random.Pick(colorPalette);
                        else
                            markingColor = random.Pick(colorPalette.Skip(0).ToList());

                        // set the marking to that color
                        markingToAdd.SetColor(markingColor);

                        // otherwise, add it to the final list.
                        newMarkings.Add(markingToAdd);
                        lastMarking = protoToAdd;
                    }
                }
            }
        }

        // at the end of all that, we should have new values for each of these, so we set the character appearance to these new values.
        return new HumanoidCharacterAppearance(newHairStyle, newHairColor, newFacialHairStyle, newHairColor, newEyeColor, newSkinColor, newMarkings);

        // helper functions:
        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }

        List<Color> GetComplementaryColors(Color color, double angle)
        {
            var hsl = Color.ToHsl(color);

            // sorry about how messy these are, but to get all random values we need to reroll for positive and negative HSL
            var hVal = hsl.X + angle;
            hVal = hVal >= 0.360 ? hVal - 0.360 : hVal;
            var positiveHSL = new Vector4((float)hVal, MathHelper.Clamp01(hsl.Y + random.Next(-20, 0) / 100f), MathHelper.Clamp01(hsl.Z + random.Next(-15, 15) / 100f), hsl.W);

            var hVal1 = hsl.X - angle;
            hVal1 = hVal1 <= 0 ? hVal1 + 0.360 : hVal1;
            var negativeHSL = new Vector4((float)hVal1, MathHelper.Clamp01(hsl.Y + random.Next(-20, 0) / 100f), MathHelper.Clamp01(hsl.Z + random.Next(-15, 15) / 100f), hsl.W);

            var c0 = Color.FromHsl(positiveHSL);
            var c1 = Color.FromHsl(negativeHSL);

            var palette = new List<Color> { color, c0, c1 };
            return palette;
        }

        // return a list of triadic complementary colors
        List<Color> GetTriadicComplementaries(Color color)
        {
            return GetComplementaryColors(color, 0.120);
        }

        // return a list of split complementary colors
        List<Color> GetSplitComplementaries(Color color)
        {
            return GetComplementaryColors(color, 0.150);
        }

        // return a list containing the base color and two copies of a single complemenary color
        List<Color> GetOneComplementary(Color color)
        {
            return GetComplementaryColors(color, 0.180);
        }

        Color SquashToSkinLuminosity(Color skinColor, Color toSquash)
        {
            var skinColorHSL = Color.ToHsl(skinColor);
            var toSquashHSL = Color.ToHsl(toSquash);

            // check if the skin color is as dark as or darker than the marking color:
            if (toSquashHSL.Z <= skinColorHSL.Z)
            {
                // if it is, don't fuck with it
                return toSquash;
            }

            // otherwise, create a new color with the H, S, and A of toSquash, but the L of skinColor
            var newColor = new Vector4(toSquashHSL.X, toSquashHSL.Y, skinColorHSL.Z, toSquashHSL.W);
            return Color.FromHsl(newColor);
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

            markingSet.EnsureSpecies(species, skinColor, markingManager);
            markingSet.EnsureSexes(sex, markingManager);
        }

        return new HumanoidCharacterAppearance(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            eyeColor,
            skinColor,
            markingSet.GetForwardEnumerator().ToList());
    }

    public bool MemberwiseEquals(ICharacterAppearance maybeOther)
    {
        if (maybeOther is not HumanoidCharacterAppearance other) return false;
        if (HairStyleId != other.HairStyleId) return false;
        if (!HairColor.Equals(other.HairColor)) return false;
        if (FacialHairStyleId != other.FacialHairStyleId) return false;
        if (!FacialHairColor.Equals(other.FacialHairColor)) return false;
        if (!EyeColor.Equals(other.EyeColor)) return false;
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
               FacialHairStyleId == other.FacialHairStyleId &&
               FacialHairColor.Equals(other.FacialHairColor) &&
               EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               Markings.SequenceEqual(other.Markings);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
