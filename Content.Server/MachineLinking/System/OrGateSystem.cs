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
                component.stateA1 = args.State;
            }
            else if (args.Port == "B1")
            {
                component.stateB1 = args.State;
            }
            else if (args.Port == "A2")
            {
                component.stateA2 = args.State;
            }
            else if (args.Port == "B2")
            {
                component.stateB2 = args.State;
            }

            // O1 = A1 || B1
            var v1 = SignalState.Low;
            if (component.stateA1 == SignalState.High || component.stateB1 == SignalState.High)
                v1 = SignalState.High;

            if (v1 != component.lastO1)
                _signalSystem.InvokePort(uid, "O1", v1);
            component.lastO1 = v1;

            // O2 = A2 || B2
            var v2 = SignalState.Low;
            if (component.stateA2 == SignalState.High || component.stateB2 == SignalState.High)
                v2 = SignalState.High;

            if (v2 != component.lastO2)
                _signalSystem.InvokePort(uid, "O2", v2);
            component.lastO2 = v2;
        }
    }
}
