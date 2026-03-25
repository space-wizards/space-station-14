using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Atmos.GasTank;

[UsedImplicitly]
public sealed class GasTankBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private GasTankWindow? _window;

    private void SetOutputPressure(float value)
    {
        SendPredictedMessage(new GasTankSetPressureMessage
        {
            Pressure = value
        });
    }

    private void ToggleInternals()
    {
        SendPredictedMessage(new GasTankToggleInternalsMessage());
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<GasTankWindow>();
        _window.Entity = Owner;
        _window.PostInitSetup();
        _window.OnOutputPressure += SetOutputPressure;
        _window.OnToggleInternals += ToggleInternals;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (EntMan.TryGetComponent(Owner, out GasTankComponent? component))
        {
            var canConnect = EntMan.System<SharedGasTankSystem>().CanConnectToInternals((Owner, component));
            _window?.Update(canConnect, component.IsConnected, component.OutputPressure);
        }

        if (state is GasTankBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.OnOutputPressure -= SetOutputPressure;
        _window?.OnToggleInternals -= ToggleInternals;
    }
}
