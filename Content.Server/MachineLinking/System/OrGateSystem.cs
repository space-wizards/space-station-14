using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using JetBrains.Annotations;

namespace Content.Server.MachineLinking.System
{
    [UsedImplicitly]
    public sealed class OrGateSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OrGateComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<OrGateComponent, SignalReceivedEvent>(OnSignalReceived);
        }
        
        private void OnInit(EntityUid uid, OrGateComponent component, ComponentInit args)
        {
            _signalSystem.EnsureReceiverPorts(uid, "A1", "B1", "A2", "B2");
            _signalSystem.EnsureTransmitterPorts(uid, "O1", "O2");
        }

        private void OnSignalReceived(EntityUid uid, OrGateComponent component, SignalReceivedEvent args)
        {
            if (args.Port == "A1")
            {
                component.StateA1 = args.State;
            }
            else if (args.Port == "B1")
            {
                component.StateB1 = args.State;
            }
            else if (args.Port == "A2")
            {
                component.StateA2 = args.State;
            }
            else if (args.Port == "B2")
            {
                component.StateB2 = args.State;
            }

            // O1 = A1 || B1
            var v1 = SignalState.Low;
            if (component.StateA1 == SignalState.High || component.StateB1 == SignalState.High)
                v1 = SignalState.High;

            if (v1 != component.LastO1)
                _signalSystem.InvokePort(uid, "O1", v1);
            component.LastO1 = v1;

            // O2 = A2 || B2
            var v2 = SignalState.Low;
            if (component.StateA2 == SignalState.High || component.StateB2 == SignalState.High)
                v2 = SignalState.High;

            if (v2 != component.LastO2)
                _signalSystem.InvokePort(uid, "O2", v2);
            component.LastO2 = v2;
        }
    }
}
