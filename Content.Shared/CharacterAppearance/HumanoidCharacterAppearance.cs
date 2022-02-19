using System;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterAppearance
{
    [Serializable, NetSerializable]
    public sealed class HumanoidCharacterAppearance : ICharacterAppearance
    {
        public HumanoidCharacterAppearance(string hairStyleId,
            Color hairColor,
            string facialHairStyleId,
            Color facialHairColor,
            Color eyeColor,
            Color skinColor)
        {
            HairStyleId = hairStyleId;
            HairColor = ClampColor(hairColor);
            FacialHairStyleId = facialHairStyleId;
            FacialHairColor = ClampColor(facialHairColor);
            EyeColor = ClampColor(eyeColor);
            SkinColor = ClampColor(skinColor);
        }

        public string HairStyleId { get; }
        public Color HairColor { get; }
        public string FacialHairStyleId { get; }
        public Color FacialHairColor { get; }
        public Color EyeColor { get; }
        public Color SkinColor { get; }

        public HumanoidCharacterAppearance WithHairStyleName(string newName)
        {
            return new(newName, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithHairColor(Color newColor)
        {
            return new(HairStyleId, newColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
        {
            return new(HairStyleId, HairColor, newName, FacialHairColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
        {
            return new(HairStyleId, HairColor, FacialHairStyleId, newColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithEyeColor(Color newColor)
        {
            return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, newColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithSkinColor(Color newColor)
        {
            return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, newColor);
        }

        public static HumanoidCharacterAppearance Default()
        {
            return new(
                HairStyles.DefaultHairStyle,
                Color.Black,
                HairStyles.DefaultFacialHairStyle,
                Color.Black,
                Color.Black,
                Color.FromHex("#C0967F")
            );
        }

        public static HumanoidCharacterAppearance Random(Sex sex)
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var prototypes = IoCManager.Resolve<SpriteAccessoryManager>();
            var hairStyles = prototypes.AccessoriesForCategory(SpriteAccessoryCategories.HumanHair);
            var facialHairStyles = prototypes.AccessoriesForCategory(SpriteAccessoryCategories.HumanHair);

            var newHairStyle = random.Pick(hairStyles).ID;

            var newFacialHairStyle = sex == Sex.Female
                ? HairStyles.DefaultFacialHairStyle
                : random.Pick(facialHairStyles).ID;

            var newHairColor = random.Pick(HairStyles.RealisticHairColors);
            newHairColor = newHairColor
                .WithRed(RandomizeColor(newHairColor.R))
                .WithGreen(RandomizeColor(newHairColor.G))
                .WithBlue(RandomizeColor(newHairColor.B));

            // TODO: Add random eye and skin color
            return new HumanoidCharacterAppearance(newHairStyle, newHairColor, newFacialHairStyle, newHairColor, Color.Black, Color.FromHex("#C0967F"));

            float RandomizeColor(float channel)
            {
                return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
            }
        }

        public static Color ClampColor(Color color)
        {
            return new(color.RByte, color.GByte, color.BByte);
        }

        public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance)
        {
            var mgr = IoCManager.Resolve<SpriteAccessoryManager>();
            var hairStyleId = appearance.HairStyleId;
            if (!mgr.IsValidAccessoryInCategory(hairStyleId, SpriteAccessoryCategories.HumanHair))
            {
                hairStyleId = HairStyles.DefaultHairStyle;
            }

            var facialHairStyleId = appearance.FacialHairStyleId;
            if (!mgr.IsValidAccessoryInCategory(facialHairStyleId, SpriteAccessoryCategories.HumanFacialHair))
            {
                facialHairStyleId = HairStyles.DefaultFacialHairStyle;
            }

            var hairColor = ClampColor(appearance.HairColor);
            var facialHairColor = ClampColor(appearance.FacialHairColor);
            var eyeColor = ClampColor(appearance.EyeColor);
            var skinColor = ClampColor(appearance.SkinColor);

            return new HumanoidCharacterAppearance(
                hairStyleId,
                hairColor,
                facialHairStyleId,
                facialHairColor,
                eyeColor,
                skinColor);
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
            return true;
        }
    }
}
