using Content.Server.Explosion.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Components;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed partial class TriggerSystem
    {
        private void InitializeSignal()
        {
            SubscribeLocalEvent<TriggerOnSignalComponent,SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<TriggerOnSignalComponent,ComponentInit>(OnInit);
        }

        private void OnSignalReceived(EntityUid uid, TriggerOnSignalComponent component, SignalReceivedEvent args)
        {
            if (args.Port != TriggerOnSignalComponent.Port)
                return;

            Trigger(uid);
        }
        private void OnInit(EntityUid uid, TriggerOnSignalComponent component, ComponentInit args)
        {
            var receiver = EnsureComp<SignalReceiverComponent>(uid);
            if (!receiver.Inputs.ContainsKey(TriggerOnSignalComponent.Port))
                receiver.AddPort(TriggerOnSignalComponent.Port);
        }
    }
}