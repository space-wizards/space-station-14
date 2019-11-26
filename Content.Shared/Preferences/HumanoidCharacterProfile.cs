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

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _name, "name", string.Empty, true);
            serializer.DataField(ref _age, "age", 18, true);
            serializer.DataField(ref _sex, "sex", Sex.Male, true);
            serializer.DataField(ref _characterAppearance, "appearance", null, true);
        }
    }
}
