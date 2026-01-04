using Content.Shared.MedicalScanner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.HealthAnalyzer.UI;

[UsedImplicitly]
public sealed class HealthAnalyzerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HealthAnalyzerWindow? _window;

    public HealthAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HealthAnalyzerWindow>();

        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is not HealthAnalyzerScannedUserMessage cast
            ||_window == null)
            return;

        _window.Populate(cast);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not HealthAnalyzerBUIState healthState
            || _window == null)
            return;

        _window.UpdateTemperature(healthState.Temperature);
    }
}
