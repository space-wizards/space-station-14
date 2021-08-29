using Content.Server.Light.Components;
using Content.Server.MachineLinking.Events;
using Robust.Shared.GameObjects;

namespace Content.Server.Light.EntitySystems
{
    public class PoweredLightSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PoweredLightComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        private void OnSignalReceived(EntityUid uid, PoweredLightComponent component, SignalReceivedEvent args)
        {
            switch (args.Port)
            {
                case "toggle":
                    component.ToggleLight();
                    break;
            }
        }
    }
}
