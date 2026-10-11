using Content.Shared.Power.Generation.EnergyCore;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Power.Generation.EnergyCore;

public sealed class EnergyCoreBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private EnergyCoreWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<EnergyCoreWindow>();
        _window.OnSetEnabled += OnSetEnabled;
        _window.OnSetOutput += OnSetOutput;
    }

    private void OnSetEnabled(bool enabled)
    {
        SendMessage(new EnergyCoreSetEnabledMessage(enabled));
    }

    private void OnSetOutput(float output)
    {
        SendMessage(new EnergyCoreSetOutputMessage(output));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is EnergyCoreUiState coreState)
            _window?.UpdateState(coreState);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _window != null)
        {
            _window.OnSetEnabled -= OnSetEnabled;
            _window.OnSetOutput -= OnSetOutput;
        }

        base.Dispose(disposing);
    }
}
