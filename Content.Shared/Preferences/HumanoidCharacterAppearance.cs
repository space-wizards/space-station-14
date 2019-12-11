using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    [Serializable, NetSerializable]
    public class HumanoidCharacterAppearance : ICharacterAppearance
    {
        public string HairStyleName;
        public Color HairColor;
        public string FacialHairStyleName;
        public Color FacialHairColor;
        public Color EyeColor;
        public Color SkinColor;

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
