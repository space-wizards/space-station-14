using Content.Shared.PlantAnalyzer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.PlantAnalyzer.UI;

[UsedImplicitly]
public sealed class PlantAnalyzerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PlantAnalyzerWindow? _window;

    public PlantAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new PlantAnalyzerWindow
        {
            Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName,
        };
        _window.OnClose += Close;
        _window.OpenToLeft();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;

        if (message is not PlantAnalyzerScannedSeedPlantInformation cast)
            return;

        _window.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }
}

