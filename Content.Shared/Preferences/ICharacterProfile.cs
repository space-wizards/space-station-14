#nullable enable

namespace Content.Shared.Preferences
{
    public interface ICharacterProfile
    {
        string Name { get; }
        ICharacterAppearance CharacterAppearance { get; }
        bool MemberwiseEquals(ICharacterProfile other);
    }
}
