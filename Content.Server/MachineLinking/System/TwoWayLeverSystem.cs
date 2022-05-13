using Content.Server.MachineLinking.Components;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;

namespace Content.Server.MachineLinking.System
{
    public sealed class TwoWayLeverSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TwoWayLeverComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<TwoWayLeverComponent, ActivateInWorldEvent>(OnActivated);
        }

        private void OnInit(EntityUid uid, TwoWayLeverComponent component, ComponentInit args)
        {
            _signalSystem.EnsureTransmitterPorts(uid, component.LeftPort, component.RightPort, component.MiddlePort);
        }

        private void OnActivated(EntityUid uid, TwoWayLeverComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            component.State = component.State switch
            {
                TwoWayLeverState.Middle => component.NextSignalLeft ? TwoWayLeverState.Left : TwoWayLeverState.Right,
                TwoWayLeverState.Right => TwoWayLeverState.Middle,
                TwoWayLeverState.Left => TwoWayLeverState.Middle,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (component.State == TwoWayLeverState.Middle)
                component.NextSignalLeft = !component.NextSignalLeft;

            if (TryComp(uid, out AppearanceComponent? appearanceComponent))
                appearanceComponent.SetData(TwoWayLeverVisuals.State, component.State);

            var port = component.State switch
            {
                TwoWayLeverState.Left => component.LeftPort,
                TwoWayLeverState.Right => component.RightPort,
                TwoWayLeverState.Middle => component.MiddlePort,
                _ => throw new ArgumentOutOfRangeException()
            };

            _signalSystem.InvokePort(uid, port);
            args.Handled = true;
        }
    }
}
