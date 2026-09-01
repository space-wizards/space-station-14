using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Localizations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI;

public sealed class GasPressureRegulatorBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private GasPressureRegulatorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GasPressureRegulatorWindow>();

        _window.SetEntity(Owner);

        _window.ThresholdPressureChanged += OnThresholdChanged;

        if (EntMan.TryGetComponent(Owner, out GasPressureRegulatorComponent? comp))
            _window.SetThresholdPressureInput(comp.Threshold);

        Update();
    }

    public override void Update()
    {
        if (_window == null)
            return;

        _window.Title = Identity.Name(Owner, EntMan);

        if (!EntMan.TryGetComponent(Owner, out GasPressureRegulatorComponent? comp))
            return;

        _window.SetThresholdPressureLabel(comp.Threshold);
        _window.UpdateInfo(comp.InletPressure, comp.OutletPressure, comp.FlowRate);
    }

    private void OnThresholdChanged(string newThreshold)
    {
        var sentThreshold = 0f;

        if (UserInputParser.TryFloat(newThreshold, out var parsedNewThreshold) && parsedNewThreshold >= 0 &&
            !float.IsInfinity(parsedNewThreshold))
        {
            sentThreshold = parsedNewThreshold;
        }

        // Autofill to zero if the user inputs an invalid value.
        _window?.SetThresholdPressureInput(sentThreshold);

        SendPredictedMessage(new GasPressureRegulatorChangeThresholdMessage(sentThreshold));
    }
}
