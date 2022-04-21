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
            if (!transmitter.Outputs.ContainsKey(SignallerComponent.Port))
                transmitter.AddPort(SignallerComponent.Port);
        }

        private void OnUseInHand(EntityUid uid, SignallerComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;
            RaiseLocalEvent(uid, new InvokePortEvent(SignallerComponent.Port), false);
            args.Handled = true;
        }   
    }
}