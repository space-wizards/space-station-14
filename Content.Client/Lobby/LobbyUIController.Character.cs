using Content.Client.Lobby.UI;
using Content.Client.Preferences.UI;
using Content.Shared.Preferences;

namespace Content.Client.Lobby;

public sealed partial class LobbyUIController
{
    /*
     * Handles the character setup GUI.
     */

    [ViewVariables] private CharacterSetupGui? _characterSetup;

    /// <summary>
    /// Loads the specified profile.
    /// </summary>
    public void LoadProfile(HumanoidCharacterProfile? profile)
    {
        if (profile == null)
        {
            _characterSetup?.Dispose();
            _characterSetup = null;
            return;
        }

        var character = EnsureGui();
    }

    private CharacterSetupGui EnsureGui()
    {
        _characterSetup ??= new CharacterSetupGui(_resourceCache, _preferencesManager,
            _prototypeManager, _configurationManager);

        _characterSetup.CloseButton.OnPressed += _ =>
        {
            // Reset sliders etc.
            _characterSetup?.UpdateControls();

            SetClothes(true);
            UpdateProfile();
            _lobby.SwitchState(LobbyGui.LobbyGuiState.Default);
        };

        _characterSetup.SaveButton.OnPressed += _ =>
        {
            _characterSetup.Save();
            ReloadProfile();
        };

        _lobby.CharacterSetupState.AddChild(_characterSetup);

        return _characterSetup;
    }
}
