using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.System
{
    [UsedImplicitly]
    public sealed class SignalButtonSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalButtonComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnInteractHand(EntityUid uid, SignalButtonComponent component, InteractHandEvent args)
        {
            RaiseLocalEvent(uid, new InvokePortEvent("pressed"), false);
            args.Handled = true;
        }
    }
}
