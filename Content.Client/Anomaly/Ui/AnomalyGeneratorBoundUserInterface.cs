using Content.Shared.Anomaly;
using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client. Anomaly.Ui;

[UsedImplicitly]
public sealed class AnomalyGeneratorBoundUserInterface : BoundUserInterface
{
    private AnomalyGeneratorWindow? _window;

    public AnomalyGeneratorBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base (owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new (Owner.Owner);

        _window.OpenCentered();
        _window.OnClose += Close;

        _window.OnGenerateButtonPressed += () =>
        {
            SendMessage(new AnomalyGeneratorGenerateButtonPressedEvent());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AnomalyGeneratorUserInterfaceState msg)
            return;
        _window?.UpdateState(msg);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }

    public void SetPowerSwitch(bool on)
    {
        SendMessage(new SharedGravityGeneratorComponent.SwitchGeneratorMessage(on));
    }
}

