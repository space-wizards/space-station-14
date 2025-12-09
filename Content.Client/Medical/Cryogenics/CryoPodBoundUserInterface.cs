using Content.Shared.MedicalScanner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Medical.Cryogenics;

[UsedImplicitly]
public sealed class CryoPodBoundUserInterface : BoundUserInterface
{
    private CryoPodWindow? _window;

    public CryoPodBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<CryoPodWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;

        if (message is HealthAnalyzerScannedUserMessage analyzerMsg)
        {
            _window.Populate(analyzerMsg);
        }

    }
}
