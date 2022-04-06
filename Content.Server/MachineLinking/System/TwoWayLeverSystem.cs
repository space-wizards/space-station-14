using System;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.System
{
    public sealed class TwoWayLeverSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TwoWayLeverComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<TwoWayLeverComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnInit(EntityUid uid, TwoWayLeverComponent component, ComponentInit args)
        {
            var transmitter = EnsureComp<SignalTransmitterComponent>(uid);
            foreach (string state in Enum.GetNames<TwoWayLeverState>())
                if (!transmitter.Outputs.ContainsKey(state))
                    transmitter.AddPort(state);
        }

        private void OnInteractHand(EntityUid uid, TwoWayLeverComponent component, InteractHandEvent args)
        {
            component.State = component.State switch
            {
                TwoWayLeverState.Middle => component.NextSignalLeft ? TwoWayLeverState.Left : TwoWayLeverState.Right,
                TwoWayLeverState.Right => TwoWayLeverState.Middle,
                TwoWayLeverState.Left => TwoWayLeverState.Middle,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (component.State == TwoWayLeverState.Middle)
            {
                component.NextSignalLeft = !component.NextSignalLeft;
            }

            if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearanceComponent))
            {
                appearanceComponent.SetData(TwoWayLeverVisuals.State, component.State);
            }

            RaiseLocalEvent(uid, new InvokePortEvent(component.State.ToString()));
            args.Handled = true;
        }
    }
}
