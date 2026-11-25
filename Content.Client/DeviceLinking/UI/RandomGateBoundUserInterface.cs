using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.DeviceLinking.UI;

[UsedImplicitly]
public sealed class RandomGateBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RandomGateSetupWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RandomGateSetupWindow>();
        _window.OnApplyPressed += OnProbabilityChanged;
    }

    private void OnProbabilityChanged(string value)
    {
        if (!float.TryParse(value, out var probability))
            return;

        SendPredictedMessage(new RandomGateProbabilityChangedMessage(probability));
    }

    public override void Update()
    {
        base.Update();

        if (_window == null || !EntMan.TryGetComponent(Owner, out RandomGateComponent? randomGate))
            return;

        _window.SetProbability(randomGate.SuccessProbability * 100);
    }
}
