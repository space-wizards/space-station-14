using Content.Shared.DeviceLinking;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.DeviceLinking.UI;

[UsedImplicitly]
public sealed class RandomGateBoundUserInterface : BoundUserInterface
{
    private RandomGateSetupWindow? _window;

    public RandomGateBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

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

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not RandomGateBoundUserInterfaceState castState || _window == null)
            return;

        _window.SetProbability(castState.SuccessProbability * 100);
    }
}
