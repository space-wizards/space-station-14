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
        }

        private void OnInteracted(EntityUid uid, SignalSwitchComponent component, InteractHandEvent args)
        {
            component.State = !component.State;
            RaiseLocalEvent(uid, new InvokePortEvent(component.State ? "on" : "off"), false);
            args.Handled = true;
        }
    }
}
