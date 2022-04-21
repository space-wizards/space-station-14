using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Content.Shared.Interaction.Events;

namespace Content.Server.MachineLinking.System
{
    [UsedImplicitly]
    public sealed class SignallerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SignallerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignallerComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnInit(EntityUid uid, SignallerComponent component, ComponentInit args)
        {
            var transmitter = EnsureComp<SignalTransmitterComponent>(uid);
            if (!transmitter.Outputs.ContainsKey("Pressed"))
                transmitter.AddPort("Pressed");
        }

        private void OnUseInHand(EntityUid uid, SignallerComponent component, UseInHandEvent args)
        {
            RaiseLocalEvent(uid, new InvokePortEvent("Pressed"), false);
            args.Handled = true;
        }   
    }
}