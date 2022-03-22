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
            SubscribeLocalEvent<SignalSwitchComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignalSwitchComponent, InteractHandEvent>(OnInteracted);
        }

        private void OnInit(EntityUid uid, SignalSwitchComponent component, ComponentInit args)
        {
            var transmitter = EnsureComp<SignalTransmitterComponent>(uid);
            foreach (string port in new[] { "On", "Off" })
                if (!transmitter.Outputs.ContainsKey(port))
                    transmitter.AddPort(port);

        }

        private void OnInteracted(EntityUid uid, SignalSwitchComponent component, InteractHandEvent args)
        {
            component.State = !component.State;
            RaiseLocalEvent(uid, new InvokePortEvent(component.State ? "On" : "Off"), false);
            args.Handled = true;
        }
    }
}
