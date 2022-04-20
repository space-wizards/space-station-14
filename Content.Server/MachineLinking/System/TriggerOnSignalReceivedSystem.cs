using Content.Server.Explosion.EntitySystems;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;

namespace Content.Server.MachineLinking.System
{
    public sealed class TriggerOnSignalReceivedSystem : EntitySystem
    {
        [Dependency] private readonly TriggerSystem _trigger = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TriggerOnSignalReceivedComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<TriggerOnSignalReceivedComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        private void OnInit(EntityUid uid, TriggerOnSignalReceivedComponent component, ComponentInit args)
        {
            _signalSystem.EnsureReceiverPorts(uid, component.TriggerPort);
        }

        private void OnSignalReceived(EntityUid uid, TriggerOnSignalReceivedComponent component, SignalReceivedEvent args)
        {
            if (args.Port == component.TriggerPort)
                _trigger.Trigger(uid);
        }
    }
}
