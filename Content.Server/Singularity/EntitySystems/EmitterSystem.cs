using Content.Server.Power.EntitySystems;
using Content.Server.Singularity.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Singularity.EntitySystems
{
    [UsedImplicitly]
    public class EmitterSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmitterComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
        }

        private static void ReceivedChanged(
            EntityUid uid,
            EmitterComponent component,
            PowerConsumerReceivedChanged args)
        {
            if (!component.IsOn)
            {
                return;
            }

            if (args.ReceivedPower < args.DrawRate)
            {
                component.PowerOff();
            }
            else
            {
                component.PowerOn();
            }
        }
    }
}
