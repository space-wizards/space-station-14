using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.UserInterface;
using Robust.Shared.Random;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class RandomGateSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomGateComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<RandomGateComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);
        SubscribeLocalEvent<RandomGateComponent, RandomGateProbabilityChangedMessage>(OnProbabilityChanged);
    }

    private void OnAfterActivatableUIOpen(Entity<RandomGateComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    private void OnProbabilityChanged(Entity<RandomGateComponent> ent, ref RandomGateProbabilityChangedMessage args)
    {
        ent.Comp.SuccessProbability = Math.Clamp(args.Probability, 0f, 100f) / 100f;
        Dirty(ent);
        UpdateUI(ent);
    }

    private void UpdateUI(Entity<RandomGateComponent> ent)
    {
        if (!_ui.HasUi(ent.Owner, RandomGateUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, RandomGateUiKey.Key, new RandomGateBoundUserInterfaceState(ent.Comp.SuccessProbability));
    }

    private void OnSignalReceived(Entity<RandomGateComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.InputPort)
            return;

        var output = _random.Prob(ent.Comp.SuccessProbability);
        if (output != ent.Comp.LastOutput)
        {
            ent.Comp.LastOutput = output;
            Dirty(ent);
            _deviceLink.SendSignal(ent.Owner, ent.Comp.OutputPort, output);
        }
    }
}
