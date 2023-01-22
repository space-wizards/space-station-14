using Content.Server.DeviceLinking.Components;
using Content.Server.MachineLinking.System;
using Content.Shared.Interaction.Events;
using JetBrains.Annotations;

namespace Content.Server.DeviceLinking.Systems
{
    [UsedImplicitly]
    public sealed class SignallerSystem : EntitySystem
    {
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SignallerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignallerComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnInit(EntityUid uid, SignallerComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSourcePorts(uid, component.Port);
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
