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

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref HairStyleName, "hairStyle", string.Empty, true);
            serializer.DataField(ref HairColor, "hairColor", Color.Black, true);
            serializer.DataField(ref FacialHairStyleName, "facialHairStyle", string.Empty, true);
            serializer.DataField(ref FacialHairColor, "facialHairColor", Color.Black, true);
            serializer.DataField(ref EyeColor, "eyeColor", Color.Black, true);
            serializer.DataField(ref SkinColor, "skinColor", Color.Pink, true);
        }
    }
}
