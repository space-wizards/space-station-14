#nullable enable
using System;
using System.Linq;
using Content.Shared.Preferences.Appearance;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    [Serializable, NetSerializable]
    public class HumanoidCharacterAppearance : ICharacterAppearance
    {
        public HumanoidCharacterAppearance(string hairStyleName,
            Color hairColor,
            string facialHairStyleName,
            Color facialHairColor,
            Color eyeColor,
            Color skinColor)
        {
            HairStyleName = hairStyleName;
            HairColor = ClampColor(hairColor);
            FacialHairStyleName = facialHairStyleName;
            FacialHairColor = ClampColor(facialHairColor);
            EyeColor = ClampColor(eyeColor);
            SkinColor = ClampColor(skinColor);
        }

        public string HairStyleName { get; }
        public Color HairColor { get; }
        public string FacialHairStyleName { get; }
        public Color FacialHairColor { get; }
        public Color EyeColor { get; }
        public Color SkinColor { get; }

        public HumanoidCharacterAppearance WithHairStyleName(string newName)
        {
            return new(newName, HairColor, FacialHairStyleName, FacialHairColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithHairColor(Color newColor)
        {
            return new(HairStyleName, newColor, FacialHairStyleName, FacialHairColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
        {
            return new(HairStyleName, HairColor, newName, FacialHairColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
        {
            return new(HairStyleName, HairColor, FacialHairStyleName, newColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithEyeColor(Color newColor)
        {
            return new(HairStyleName, HairColor, FacialHairStyleName, FacialHairColor, newColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithSkinColor(Color newColor)
        {
            return new(HairStyleName, HairColor, FacialHairStyleName, FacialHairColor, EyeColor, newColor);
        }

        public static HumanoidCharacterAppearance Default()
        {
            return new(
                "Bald",
                Color.Black,
                "Shaved",
                Color.Black,
                Color.Black,
                Color.FromHex("#C0967F")
            );
        }

        public static HumanoidCharacterAppearance Random(Sex sex)
        {
            var random = IoCManager.Resolve<IRobustRandom>();

            var newHairStyle = random.Pick(HairStyles.HairStylesMap.Keys.ToList());

            var newFacialHairStyle = sex == Sex.Female
                ? HairStyles.DefaultFacialHairStyle
                : random.Pick(HairStyles.FacialHairStylesMap.Keys.ToList());

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
            string hairStyleName;
            if (!HairStyles.HairStylesMap.ContainsKey(appearance.HairStyleName))
            {
                hairStyleName = HairStyles.DefaultHairStyle;
            }
            else
            {
                hairStyleName = appearance.HairStyleName;
            }

            string facialHairStyleName;
            if (!HairStyles.FacialHairStylesMap.ContainsKey(appearance.FacialHairStyleName))
            {
                facialHairStyleName = HairStyles.DefaultFacialHairStyle;
            }
            else
            {
                facialHairStyleName = appearance.FacialHairStyleName;
            }

            var hairColor = ClampColor(appearance.HairColor);
            var facialHairColor = ClampColor(appearance.FacialHairColor);
            var eyeColor = ClampColor(appearance.EyeColor);
            var skinColor = ClampColor(appearance.SkinColor);

            return new HumanoidCharacterAppearance(
                hairStyleName,
                hairColor,
                facialHairStyleName,
                facialHairColor,
                eyeColor,
                skinColor);
        }

        public bool MemberwiseEquals(ICharacterAppearance maybeOther)
        {
            if (maybeOther is not HumanoidCharacterAppearance other) return false;
            if (HairStyleName != other.HairStyleName) return false;
            if (!HairColor.Equals(other.HairColor)) return false;
            if (FacialHairStyleName != other.FacialHairStyleName) return false;
            if (!FacialHairColor.Equals(other.FacialHairColor)) return false;
            if (!EyeColor.Equals(other.EyeColor)) return false;
            if (!SkinColor.Equals(other.SkinColor)) return false;
            return true;
        }
    }
}
