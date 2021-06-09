using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class PowerReceiverSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PowerReceiverComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<PowerReceiverComponent, PhysicsBodyTypeChangedEvent>();
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            PowerReceiverComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.AnchorUpdate();
        }
    }
}
