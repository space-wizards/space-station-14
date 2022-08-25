using System.Linq;
using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid.Species;
using Content.Shared.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid
{
    [Serializable, NetSerializable]
    public sealed class HumanoidCharacterAppearance : ICharacterAppearance
    {
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

        public string HairStyleId { get; }
        public Color HairColor { get; }
        public string FacialHairStyleId { get; }
        public Color FacialHairColor { get; }
        public Color EyeColor { get; }
        public Color SkinColor { get; }
        public List<Marking> Markings { get; }

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

        public static HumanoidCharacterAppearance Default()
        {
            return new(
                HairStyles.DefaultHairStyle,
                Color.Black,
                HairStyles.DefaultFacialHairStyle,
                Color.Black,
                Color.Black,
                Color.FromHex("#C0967F"),
                new ()
            );
        }

        public static HumanoidCharacterAppearance Random(string species, Sex sex)
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var markingManager = IoCManager.Resolve<MarkingManager>();
            var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, species).Keys.ToList();
            var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, species).Keys.ToList();

            var newHairStyle = random.Pick(hairStyles);

            var newFacialHairStyle = sex == Sex.Female
                ? HairStyles.DefaultFacialHairStyle
                : random.Pick(facialHairStyles);

            var newHairColor = random.Pick(HairStyles.RealisticHairColors);
            newHairColor = newHairColor
                .WithRed(RandomizeColor(newHairColor.R))
                .WithGreen(RandomizeColor(newHairColor.G))
                .WithBlue(RandomizeColor(newHairColor.B));

            // TODO: Add random eye and skin color
            return new HumanoidCharacterAppearance(newHairStyle, newHairColor, newFacialHairStyle, newHairColor, Color.Black, Color.FromHex("#C0967F"), new ());

            float RandomizeColor(float channel)
            {
                return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
            }
        }

        public static Color ClampColor(Color color)
        {
            return new(color.RByte, color.GByte, color.BByte);
        }

        public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, string species)
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
                hairStyleId = HairStyles.DefaultFacialHairStyle;
            }

            var markingSet = new MarkingSet();
            var skinColor = appearance.SkinColor;
            if (proto.TryIndex(species, out SpeciesPrototype? speciesProto))
            {
                markingSet = new MarkingSet(appearance.Markings, speciesProto.MarkingPoints, markingManager, proto);
                markingSet.EnsureValid(markingManager);
                markingSet.FilterSpecies(species, markingManager);

                switch (speciesProto.SkinColoration)
                {
                    case SpeciesSkinColor.HumanToned:
                        if (!Humanoid.SkinColor.VerifyHumanSkinTone(skinColor))
                        {
                            skinColor = Humanoid.SkinColor.ValidHumanSkinTone;
                        }

                        break;
                    case SpeciesSkinColor.TintedHues:
                        if (!Humanoid.SkinColor.VerifyTintedHues(skinColor))
                        {
                            skinColor = Humanoid.SkinColor.ValidTintedHuesSkinTone(skinColor);
                        }

                        break;
                }
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
    }
}
