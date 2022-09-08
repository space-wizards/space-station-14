using Content.Client.Eui;
using Content.Client.UserInterface;
using Content.Shared.Eui;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Client.Weapons.Melee;

public sealed class MeleeWeaponEui : BaseEui
{
    private readonly StatsWindow _window;

    public MeleeWeaponEui()
    {
        _window = new StatsWindow();
        _window.Title = "Melee stats";
        _window.OpenCentered();
        _window.OnClose += Closed;
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not MeleeValuesEuiState eui)
            return;

        _window.UpdateValues(eui.Headers, eui.Values);
    }
}
