using Content.Shared.Preferences;

namespace Content.Client.Interfaces
{
    public interface IClientPreferencesManager
    {
        void Initialize();
        GameSettings Settings { get; }
        PlayerPreferences Preferences { get; }
        void SelectCharacter(int slot);
        void UpdateCharacter(ICharacterProfile profile, int slot);
    }
}
