using Content.Shared.Preferences;

namespace Content.Client.Interfaces
{
    public interface IClientPreferencesManager
    {
        void Initialize();
        GameSettings Settings { get; }
        PlayerPreferences Preferences { get; }
        void SavePreferences();
    }
}
