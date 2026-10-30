using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.UserInterface;
using Robust.Shared.Timing;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class RandomGateSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnAfterActivatableUIOpen(Entity<RandomGateComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnProbabilityChanged(Entity<RandomGateComponent> ent, ref RandomGateProbabilityChangedMessage args)
    {
        if (!args.Probability.IsValid())
            return;

        ent.Comp.SuccessProbability = Math.Clamp(args.Probability, 0f, 100f) / 100f;
        DirtyField(ent.AsNullable(), nameof(RandomGateComponent.SuccessProbability));
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<RandomGateComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.InputPort)
            return;

        var output = SharedRandomExtensions.PredictedProb(_timing, ent.Comp.SuccessProbability, GetNetEntity(ent));
        if (output == ent.Comp.LastOutput)
            return;

        ent.Comp.LastOutput = output;
        DirtyField(ent.AsNullable(), nameof(RandomGateComponent.LastOutput));
        _deviceLink.SendSignal(ent.Owner, ent.Comp.OutputPort, output);
    }

    private void UpdateUI(Entity<RandomGateComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, RandomGateUiKey.Key, out var ui))
            ui.Update();
    }
}
