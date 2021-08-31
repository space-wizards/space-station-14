using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Models;
using Content.Server.Stunnable.Components;
using Content.Shared.MachineLinking;
using Content.Shared.Notification.Managers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Conveyor
{
    public class ConveyorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ConveyorComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<ConveyorComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<ConveyorComponent, LinkAttemptEvent>(OnLinkAttempt);
        }

        private void OnLinkAttempt(EntityUid uid, ConveyorComponent component, LinkAttemptEvent args)
        {
            if (args.TransmitterComponent.Outputs.GetPort(args.TransmitterPort).Signal is TwoWayLeverSignal signal &&
                signal != TwoWayLeverSignal.Middle)
            {
                args.Cancel();
                if (args.Attemptee.TryGetComponent<StunnableComponent>(out var stunnableComponent))
                {
                    stunnableComponent.Paralyze(2);
                    component.Owner.PopupMessage(args.Attemptee, Loc.GetString("conveyor-component-failed-link"));
                }
            }
        }

        private void OnPortDisconnected(EntityUid uid, ConveyorComponent component, PortDisconnectedEvent args)
        {
            component.SetState(TwoWayLeverSignal.Middle);
        }

        private void OnSignalReceived(EntityUid uid, ConveyorComponent component, SignalReceivedEvent args)
        {
            switch (args.Port)
            {
                case "state":
                    component.SetState((TwoWayLeverSignal) args.Value!);
                    break;
            }
        }
    }
}
