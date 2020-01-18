using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    [Serializable, NetSerializable]
    public class HumanoidCharacterProfile : ICharacterProfile
    {
        public HumanoidCharacterProfile(string name,
            int age,
            Sex sex,
            HumanoidCharacterAppearance appearance)
        {
            Name = name;
            Age = age;
            Sex = sex;
            Appearance = appearance;
        }

        public static HumanoidCharacterProfile Default()
        {
            return new HumanoidCharacterProfile("John Doe", 18, Sex.Male, HumanoidCharacterAppearance.Default());
        }

        public string Name { get; }
        public int Age { get; }
        public Sex Sex { get; }
        public ICharacterAppearance CharacterAppearance => Appearance;
        public HumanoidCharacterAppearance Appearance { get; }

        public HumanoidCharacterProfile WithName(string name)
        {
            return new HumanoidCharacterProfile(name, Age, Sex, Appearance);
        }

        public HumanoidCharacterProfile WithAge(int age)
        {
            return new HumanoidCharacterProfile(Name, age, Sex, Appearance);
        }

        public HumanoidCharacterProfile WithSex(Sex sex)
        {
            return new HumanoidCharacterProfile(Name, Age, sex, Appearance);
        }

        public HumanoidCharacterProfile WithCharacterAppearance(HumanoidCharacterAppearance appearance)
        {
            return new HumanoidCharacterProfile(Name, Age, Sex, appearance);
        }

        public string Summary => $"{Name}, {Age} years old {Sex.ToString().ToLower()} human.\nOccupation: to be implemented.";

        public bool MemberwiseEquals(ICharacterProfile maybeOther)
        {
            if (!(maybeOther is HumanoidCharacterProfile other)) return false;
            if (Name != other.Name) return false;
            if (Age != other.Age) return false;
            if (Sex != other.Sex) return false;
            return Appearance.MemberwiseEquals(other.Appearance);
        }
    }
}
