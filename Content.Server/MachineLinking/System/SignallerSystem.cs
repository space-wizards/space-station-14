using Content.Server.MachineLinking.Components;
using JetBrains.Annotations;
using Content.Shared.Interaction.Events;

namespace Content.Server.MachineLinking.System
{
    [UsedImplicitly]
    public sealed class SignallerSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SignallerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignallerComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnInit(EntityUid uid, SignallerComponent component, ComponentInit args)
        {
            _signalSystem.EnsureTransmitterPorts(uid, component.Port);
        }

        private void OnUseInHand(EntityUid uid, SignallerComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;
            _signalSystem.InvokePort(uid, component.Port);
            args.Handled = true;
        }   
    }
}
