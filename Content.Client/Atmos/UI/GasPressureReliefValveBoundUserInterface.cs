using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Localizations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI;

public sealed class GasPressureReliefValveBoundUserInterface : BoundUserInterface
{
    private GasPressureReliefValveWindow? _window;

    public GasPressureReliefValveBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GasPressureReliefValveWindow>();

        _window.ThresholdPressureChanged += OnThresholdChanged;
    }

    // TODO: This needs an Update() method to update the UI when the state changes.
    // This will allow us to provide flow rate and opening/closing info.

    private void OnThresholdChanged(string newThreshold)
    {
        var sentThreshold = UserInputParser.TryFloat(newThreshold, out var parsedNewThreshold)
            ? parsedNewThreshold
            : 0f;
        if (parsedNewThreshold < 0)
            sentThreshold = 0;

        SendPredictedMessage(new GasPressureReliefValveChangeThresholdMessage(sentThreshold));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GasPressureReliefValveBoundUserInterfaceState valveState || _window == null)
            return;

        _window.Title = valveState.ValveLabel;
        _window.SetThreshold(valveState.ThresholdPressure);
    }
}
