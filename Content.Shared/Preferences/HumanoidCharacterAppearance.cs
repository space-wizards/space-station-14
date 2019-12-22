using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    [Serializable, NetSerializable]
    public class HumanoidCharacterAppearance : ICharacterAppearance
    {
        public string HairStyleName { get; set; }
        public Color HairColor { get; set; }
        public string FacialHairStyleName { get; set; }
        public Color FacialHairColor { get; set; }
        public Color EyeColor { get; set; }
        public Color SkinColor { get; set; }

        public static HumanoidCharacterAppearance Default()
        {
            return new HumanoidCharacterAppearance
            {
                HairStyleName = "Bald",
                HairColor = Color.Black,
                FacialHairStyleName = "Shaved",
                FacialHairColor = Color.Black,
                EyeColor = Color.Black,
                SkinColor = Color.Black
            };
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
