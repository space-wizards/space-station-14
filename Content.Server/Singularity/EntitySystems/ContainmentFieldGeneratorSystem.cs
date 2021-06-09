using Content.Server.Singularity.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Singularity.EntitySystems
{
    public sealed class ContainmentFieldGeneratorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<ContainmentFieldGeneratorComponent, PhysicsBodyTypeChangedEvent>();
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            ContainmentFieldGeneratorComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.OnAnchoredChanged();
        }
    }
}
