using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.DeviceLinking.Systems;

public sealed class RandomGateSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomGateComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<RandomGateComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);
        SubscribeLocalEvent<RandomGateComponent, RandomGateProbabilityChangedMessage>(OnProbabilityChanged);
    }

    private void OnSignalReceived(EntityUid uid, RandomGateComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port != component.InputPort)
            return;

        var output = _random.Prob(component.SuccessProbability);
        if (output != component.LastOutput)
        {
            component.LastOutput = output;
            _deviceLink.SendSignal(uid, component.OutputPort, output);
        }
    }

    private void OnAfterActivatableUIOpen(EntityUid uid, RandomGateComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUI(uid, component);
    }

    private void OnProbabilityChanged(EntityUid uid, RandomGateComponent component, RandomGateProbabilityChangedMessage args)
    {
        component.SuccessProbability = Math.Clamp(args.Probability, 0f, 1f);
        UpdateUI(uid, component);
    }

    private void UpdateUI(EntityUid uid, RandomGateComponent component)
    {
        if (!_ui.HasUi(uid, RandomGateUiKey.Key))
            return;

        _ui.SetUiState(uid, RandomGateUiKey.Key, new RandomGateBoundUserInterfaceState(component.SuccessProbability));
    }
}
