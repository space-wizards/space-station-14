using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.System
{
    public sealed class SignalSwitchSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalSwitchComponent, InteractHandEvent>(OnInteracted);
            SubscribeLocalEvent<SignalSwitchComponent, SignalValueRequestedEvent>(OnSignalValueRequested);
        }

        private void OnSignalValueRequested(EntityUid uid, SignalSwitchComponent component, SignalValueRequestedEvent args)
        {
            if (args.Port == "state")
            {
                args.Handled = true;
                args.Signal = component.State;
            }
        }

        private void OnInteracted(EntityUid uid, SignalSwitchComponent component, InteractHandEvent args)
        {
            component.State = !component.State;
            RaiseLocalEvent(uid, new InvokePortEvent("state", component.State), false);
            RaiseLocalEvent(uid, new InvokePortEvent("stateChange"), false);
            args.Handled = true;
        }
    }
}
