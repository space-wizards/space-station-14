using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    [Serializable, NetSerializable]
    public class HumanoidCharacterProfile : ICharacterProfile
    {
        public static HumanoidCharacterProfile Default()
        {
            return new HumanoidCharacterProfile
            {
                Name = "John Doe",
                Age = 18,
                Sex = Sex.Male,
                CharacterAppearance = HumanoidCharacterAppearance.Default()
            };
        }

        public string Name { get; set; }
        public int Age { get; set; }
        public Sex Sex { get; set; }
        public ICharacterAppearance CharacterAppearance { get; set; }

        public bool MemberwiseEquals(ICharacterProfile maybeOther)
        {
            if (!(maybeOther is HumanoidCharacterProfile other)) return false;
            if (Name != other.Name) return false;
            if (Age != other.Age) return false;
            if (Sex != other.Sex) return false;
            if (CharacterAppearance is null)
                return other.CharacterAppearance is null;
            return CharacterAppearance.MemberwiseEquals(other.CharacterAppearance);
        }
    }
}
