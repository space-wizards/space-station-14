using System.Linq;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid
{
    [DataDefinition]
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

        [DataField("hair")]
        public string HairStyleId { get; }

        [DataField("hairColor")]
        public Color HairColor { get; }

        [DataField("facialHair")]
        public string FacialHairStyleId { get; }

        [DataField("facialHairColor")]
        public Color FacialHairColor { get; }

        [DataField("eyeColor")]
        public Color EyeColor { get; }

        [DataField("skinColor")]
        public Color SkinColor { get; }

        [DataField("markings")]
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
                Humanoid.SkinColor.ValidHumanSkinTone,
                new ()
            );
        }

        public static HumanoidCharacterAppearance DefaultWithSpecies(string species)
        {
            var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);
            var skinColor = speciesPrototype.SkinColoration switch
            {
                HumanoidSkinColor.HumanToned => Humanoid.SkinColor.HumanSkinTone(speciesPrototype.DefaultHumanSkinTone),
                HumanoidSkinColor.Hues => speciesPrototype.DefaultSkinTone,
                HumanoidSkinColor.TintedHues => Humanoid.SkinColor.TintedHues(speciesPrototype.DefaultSkinTone),
                _ => Humanoid.SkinColor.ValidHumanSkinTone
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
            var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, species).Keys.ToList();
            var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, species).Keys.ToList();

            var newHairStyle = hairStyles.Count > 0
                ? random.Pick(hairStyles)
                : HairStyles.DefaultHairStyle;

            var newFacialHairStyle = facialHairStyles.Count == 0 || sex == Sex.Female
                ? HairStyles.DefaultFacialHairStyle
                : random.Pick(facialHairStyles);

            var newHairColor = random.Pick(HairStyles.RealisticHairColors);
            newHairColor = newHairColor
                .WithRed(RandomizeColor(newHairColor.R))
                .WithGreen(RandomizeColor(newHairColor.G))
                .WithBlue(RandomizeColor(newHairColor.B));

            // TODO: Add random markings

            var newEyeColor = random.Pick(RealisticEyeColors);

            var skinType = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species).SkinColoration;

            var newSkinColor = Humanoid.SkinColor.ValidHumanSkinTone;
            switch (skinType)
            {
                case HumanoidSkinColor.HumanToned:
                    var tone = random.Next(0, 100);
                    newSkinColor = Humanoid.SkinColor.HumanSkinTone(tone);
                    break;
                case HumanoidSkinColor.Hues:
                case HumanoidSkinColor.TintedHues:
                    var rbyte = random.Next(0, 255);
                    var gbyte = random.Next(0, 255);
                    var bbyte = random.Next(0, 255);
                    newSkinColor = new Color(rbyte, gbyte, bbyte);
                    break;
            }

            if (skinType == HumanoidSkinColor.TintedHues)
            {
                newSkinColor = Humanoid.SkinColor.ValidTintedHuesSkinTone(newSkinColor);
            }

            return new HumanoidCharacterAppearance(newHairStyle, newHairColor, newFacialHairStyle, newHairColor, newEyeColor, newSkinColor, new ());

            float RandomizeColor(float channel)
            {
                return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
            }
        }

        public static Color ClampColor(Color color)
        {
            return new(color.RByte, color.GByte, color.BByte);
        }

        public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, string species, string[] sponsorMarkings)
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
            
            // Corvax-Sponsors-Start
            if (proto.TryIndex(hairStyleId, out MarkingPrototype? hairProto) &&
                hairProto.SponsorOnly &&
                !sponsorMarkings.Contains(hairStyleId))
            {
                hairStyleId = HairStyles.DefaultHairStyle;
            }
            // Corvax-Sponsors-End

            if (!markingManager.MarkingsByCategory(MarkingCategories.FacialHair).ContainsKey(facialHairStyleId))
            {
                facialHairStyleId = HairStyles.DefaultFacialHairStyle;
            }
            
            // Corvax-Sponsors-Start
            if (proto.TryIndex(facialHairStyleId, out MarkingPrototype? facialHairProto) &&
                facialHairProto.SponsorOnly &&
                !sponsorMarkings.Contains(facialHairStyleId))
            {
                facialHairStyleId = HairStyles.DefaultFacialHairStyle;
            }
            // Corvax-Sponsors-End

            var markingSet = new MarkingSet();
            var skinColor = appearance.SkinColor;
            if (proto.TryIndex(species, out SpeciesPrototype? speciesProto))
            {
                markingSet = new MarkingSet(appearance.Markings, speciesProto.MarkingPoints, markingManager, proto);
                markingSet.EnsureValid(markingManager);
                markingSet.FilterSpecies(species, markingManager);
                markingSet.FilterSponsor(sponsorMarkings, markingManager); // Corvax-Sponsors

                switch (speciesProto.SkinColoration)
                {
                    case HumanoidSkinColor.HumanToned:
                        if (!Humanoid.SkinColor.VerifyHumanSkinTone(skinColor))
                        {
                            skinColor = Humanoid.SkinColor.ValidHumanSkinTone;
                        }

                        break;
                    case HumanoidSkinColor.TintedHues:
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
