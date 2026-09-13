using Content.Shared.DeviceLinking;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.DeviceLinking.Events;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    [SubscribeLocalEvent]
    private void SignalOnTriggerInit(Entity<SignalOnTriggerComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent.Owner, ent.Comp.Port);
    }

    [SubscribeLocalEvent]
    private void TriggerOnSignalInit(Entity<TriggerOnSignalComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.Port);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<TriggerOnSignalComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.Port)
            return;

        Trigger(ent.Owner, args.Trigger, ent.Comp.KeyOut);
    }
}

public sealed partial class SignalOnTriggerSystem : XOnTriggerSystem<SignalOnTriggerComponent>
{
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;

    protected override void OnTrigger(Entity<SignalOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _deviceLink.InvokePort(ent.Owner, ent.Comp.Port);
        args.Handled = true;
    }
}
