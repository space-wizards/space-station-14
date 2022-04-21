using Content.Server.Explosion.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Components;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed partial class TriggerSystem
    {
        private void InitializeSignal()
        {
            SubscribeLocalEvent<SignalTriggerComponent,SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<SignalTriggerComponent,ComponentInit>(OnInit);
        }

        private void OnSignalReceived(EntityUid uid, SignalTriggerComponent component, SignalReceivedEvent args)
        {
            if (args.Port != SignalTriggerComponent.Port)
                return;

            Trigger(uid);
        }
        private void OnInit(EntityUid uid, SignalTriggerComponent component, ComponentInit args)
        {
            var receiver = AddComp<SignalReceiverComponent>(uid);
            if (!receiver.Inputs.ContainsKey(SignalTriggerComponent.Port))
                receiver.AddPort(SignalTriggerComponent.Port);
        }
    }
}