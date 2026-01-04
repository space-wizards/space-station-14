using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.DeviceLinking.Events;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeSignal()
    {
        SubscribeLocalEvent<SignalOnTriggerComponent, ComponentInit>(SignalOnTriggerInit);
        SubscribeLocalEvent<TriggerOnSignalComponent, ComponentInit>(TriggerOnSignalInit);

        SubscribeLocalEvent<SignalOnTriggerComponent, TriggerEvent>(HandleSignalOnTrigger);
        SubscribeLocalEvent<TriggerOnSignalComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void SignalOnTriggerInit(Entity<SignalOnTriggerComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent.Owner, ent.Comp.Port);
    }

    private void TriggerOnSignalInit(Entity<TriggerOnSignalComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.Port);
    }

    private void HandleSignalOnTrigger(Entity<SignalOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Keys != null && !ent.Comp.KeysIn.Overlaps(args.Keys))
            return;

        _deviceLink.InvokePort(ent.Owner, ent.Comp.Port);
        args.Handled = true;
    }

    private void OnSignalReceived(Entity<TriggerOnSignalComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.Port)
            return;

        Trigger(ent.Owner, args.Trigger, ent.Comp.KeyOut);
    }
}
