using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.IdentityManagement;
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
        Update();
    }

    public override void Update()
    {
        if (_window == null)
            return;

        _window.Title = Identity.Name(Owner, EntMan);

        if (!EntMan.TryGetComponent(Owner, out GasPressureReliefValveComponent? valveComponent))
            return;

        // TODO: This bulldozes the window title we set when making the window in the first place,
        // remember to nuke that when finished
        _window.SetThresholdPressureLabel(valveComponent.Threshold);
        _window.SetValveStatus(valveComponent.Enabled);
        _window.SetFlowRate(valveComponent.FlowRate);
    }

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

        _window.SetThresholdPressureLabel(valveState.ThresholdPressure);
    }
}
