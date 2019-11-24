using Content.Shared.Preferences;

namespace Content.Client.Interfaces
{
    public interface IClientPreferencesManager
    {
        GameSettings GetSettings();
        PlayerPreferences GetPreferences();
        void SavePreferences();
    }
}
