using Content.Server.Power.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Power.EntitySystems
{
    public sealed class PowerReceiverSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ApcPowerReceiverComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            ApcPowerReceiverComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.AnchorUpdate();
        }
    }
}
