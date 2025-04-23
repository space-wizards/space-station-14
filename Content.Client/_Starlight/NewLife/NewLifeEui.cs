using System.Linq;
using Content.Client.Eui;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.Starlight.NewLife;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using Content.Shared.Humanoid.Markings;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.NewLife;

[UsedImplicitly]
public sealed class NewLifeEui : BaseEui
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;

    private readonly NewLifeWindow _window;

    public NewLifeEui()
    {
        _window = new NewLifeWindow(_preferencesManager);

        _window.SelectCharacter += slot =>
        {
            _preferencesManager.SelectCharacter(slot);
            _window.ReloadCharacterPickers();
        };
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not NewLifeEuiState newLifeEuiState)
            return;
        _window.ReloadCharacterPickers(newLifeEuiState.UsedSlots);
    }
}
