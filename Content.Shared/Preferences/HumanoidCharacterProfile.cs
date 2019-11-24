using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    [Serializable, NetSerializable]
    public class HumanoidCharacterProfile : ICharacterProfile
    {
        private string _name;
        private int _age;
        private Gender _gender;
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

        public Gender Gender
        {
            get => _gender;
            set => _gender = value;
        }

        public ICharacterAppearance CharacterAppearance
        {
            get => _characterAppearance;
            set => _characterAppearance = value;
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _name, "name", string.Empty, true);
            serializer.DataField(ref _age, "age", 18, true);
            serializer.DataField(ref _gender, "gender", Gender.Male, true);
            serializer.DataField(ref _characterAppearance, "appearance", null, true);
        }
    }
}
