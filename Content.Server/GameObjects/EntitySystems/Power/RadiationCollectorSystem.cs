using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class RadiationCollectorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RadiationCollectorComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<RadiationCollectorComponent, PhysicsBodyTypeChangedEvent>();
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            RadiationCollectorComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.OnAnchoredChanged();
        }
    }
}
