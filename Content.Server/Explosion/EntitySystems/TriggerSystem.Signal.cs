using Content.Server.Explosion.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.System;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed partial class TriggerSystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
            
        private void InitializeSignal()
        {
            SubscribeLocalEvent<TriggerOnSignalComponent,SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<TriggerOnSignalComponent,ComponentInit>(OnInit);
        }

        private void OnSignalReceived(EntityUid uid, TriggerOnSignalComponent component, SignalReceivedEvent args)
        {
            if (args.Port != component.Port)
                return;

            Trigger(uid);
        }
        private void OnInit(EntityUid uid, TriggerOnSignalComponent component, ComponentInit args)
        {
            _signalSystem.EnsureReceiverPorts(uid, component.Port);
        }
    }
}
