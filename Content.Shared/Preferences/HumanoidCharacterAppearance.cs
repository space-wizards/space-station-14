using System;
using Robust.Shared.Maths;
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
            HairColor = hairColor;
            FacialHairStyleName = facialHairStyleName;
            FacialHairColor = facialHairColor;
            EyeColor = eyeColor;
            SkinColor = skinColor;
        }

        public string HairStyleName { get; }
        public Color HairColor { get; }
        public string FacialHairStyleName { get; }
        public Color FacialHairColor { get; }
        public Color EyeColor { get; }
        public Color SkinColor { get; }

        public HumanoidCharacterAppearance WithHairStyleName(string newName)
        {
            return new HumanoidCharacterAppearance(newName, HairColor, FacialHairStyleName, FacialHairColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithHairColor(Color newColor)
        {
            return new HumanoidCharacterAppearance(HairStyleName, newColor, FacialHairStyleName, FacialHairColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
        {
            return new HumanoidCharacterAppearance(HairStyleName, HairColor, newName, FacialHairColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
        {
            return new HumanoidCharacterAppearance(HairStyleName, HairColor, FacialHairStyleName, newColor, EyeColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithEyeColor(Color newColor)
        {
            return new HumanoidCharacterAppearance(HairStyleName, HairColor, FacialHairStyleName, FacialHairColor, newColor, SkinColor);
        }

        public HumanoidCharacterAppearance WithSkinColor(Color newColor)
        {
            return new HumanoidCharacterAppearance(HairStyleName, HairColor, FacialHairStyleName, FacialHairColor, EyeColor, newColor);
        }

        public static HumanoidCharacterAppearance Default()
        {
            return new HumanoidCharacterAppearance
            (
                "Bald",
                Color.Black,
                "Shaved",
                Color.Black,
                Color.Black,
                Color.Black
            );
        }

        public bool MemberwiseEquals(ICharacterAppearance maybeOther)
        {
            if (!(maybeOther is HumanoidCharacterAppearance other)) return false;
            if (HairStyleName != other.HairStyleName) return false;
            if (HairColor != other.HairColor) return false;
            if (FacialHairStyleName != other.FacialHairStyleName) return false;
            if (FacialHairColor != other.FacialHairColor) return false;
            if (EyeColor != other.EyeColor) return false;
            if (SkinColor != other.SkinColor) return false;
            return true;
        }
    }
}
