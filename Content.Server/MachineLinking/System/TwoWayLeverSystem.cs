using System;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.MachineLinking.System
{
    public class TwoWayLeverSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TwoWayLeverComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<TwoWayLeverComponent, SignalValueRequestedEvent>(OnSignalValueRequested);
        }

        private void OnSignalValueRequested(EntityUid uid, TwoWayLeverComponent component, SignalValueRequestedEvent args)
        {
            args.Signal = component.State;
            args.Handled = true;
        }

        private void OnInteractHand(EntityUid uid, TwoWayLeverComponent component, InteractHandEvent args)
        {
            component.State = component.State switch
            {
                TwoWayLeverSignal.Middle => component.NextSignalLeft ? TwoWayLeverSignal.Left : TwoWayLeverSignal.Right,
                TwoWayLeverSignal.Right => TwoWayLeverSignal.Middle,
                TwoWayLeverSignal.Left => TwoWayLeverSignal.Middle,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (component.State == TwoWayLeverSignal.Middle)
            {
                component.NextSignalLeft = !component.NextSignalLeft;
            }

            if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearanceComponent))
            {
                appearanceComponent.SetData(TwoWayLeverVisuals.State, component.State);
            }

            RaiseLocalEvent(uid, new InvokePortEvent("state", component.State));
            args.Handled = true;
        }
    }
}
