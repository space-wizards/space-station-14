using Content.Shared.Preferences;

namespace Content.Client.Interfaces
{
    public interface IClientPreferencesManager
    {
        GameSettings Settings { get; }
        PlayerPreferences Preferences { get; }
        void SavePreferences();
    }
}
