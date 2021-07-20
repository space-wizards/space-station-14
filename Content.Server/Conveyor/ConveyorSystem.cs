using Content.Server.MachineLinking.Events;
using Content.Shared.MachineLinking;
using Robust.Shared.GameObjects;

namespace Content.Server.Conveyor
{
    public class ConveyorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ConveyorComponent, SignalReceivedEvent>(OnSignalReceived);
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
