using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using Content.Server.MachineLinking.Events;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;

namespace Content.Server.DeviceLinking.Systems
{
    [UsedImplicitly]
    public sealed class OrGateSystem : EntitySystem
    {

        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OrGateComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<OrGateComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        private void OnInit(EntityUid uid, OrGateComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSinkPorts(uid, "A1", "B1", "A2", "B2");
            _signalSystem.EnsureSourcePorts(uid, "O1", "O2");
        }

        private void OnSignalReceived(EntityUid uid, OrGateComponent component, ref SignalReceivedEvent args)
        {
            var state = SignalState.Momentary;
            args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

            switch (args.Port)
            {
                case "A1":
                    component.StateA1 = state;
                    break;
                case "B1":
                    component.StateB1 = state;
                    break;
                case "A2":
                    component.StateA2 = state;
                    break;
                case "B2":
                    component.StateB2 = state;
                    break;
            }

            // O1 = A1 || B1
            var v1 = SignalState.Low;
            if (component.StateA1 == SignalState.High || component.StateB1 == SignalState.High)
                v1 = SignalState.High;

            if (v1 != component.LastO1)
            {
                var data = new NetworkPayload
                {
                    [DeviceNetworkConstants.LogicState] = v1
                };

                _signalSystem.InvokePort(uid, "O1", data);
            }

            component.LastO1 = v1;

            // O2 = A2 || B2
            var v2 = SignalState.Low;
            if (component.StateA2 == SignalState.High || component.StateB2 == SignalState.High)
                v2 = SignalState.High;

            if (v2 != component.LastO2)
            {
                var data = new NetworkPayload
                {
                    [DeviceNetworkConstants.LogicState] = v2
                };

                _signalSystem.InvokePort(uid, "O2", data);
            }
            component.LastO2 = v2;
        }
    }
}
