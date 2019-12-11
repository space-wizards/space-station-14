using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    [Serializable, NetSerializable]
    public class HumanoidCharacterProfile : ICharacterProfile
    {
        private string _name;
        private int _age;
        private Sex _sex;
        private ICharacterAppearance _characterAppearance;

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public int Age
        {
            get => _age;
            set => _age = value;
        }

        public Sex Sex
        {
            get => _sex;
            set => _sex = value;
        }

        public ICharacterAppearance CharacterAppearance
        {
            get => _characterAppearance;
            set => _characterAppearance = value;
        }

        public bool MemberwiseEquals(ICharacterProfile maybeOther)
        {
            if (!(maybeOther is HumanoidCharacterProfile other)) return false;
            if (Name != other.Name) return false;
            if (Age != other.Age) return false;
            if (Sex != other.Sex) return false;
            if (CharacterAppearance is null && other.CharacterAppearance is null) return true;
            return CharacterAppearance.MemberwiseEquals(other.CharacterAppearance);
        }
    }
}
