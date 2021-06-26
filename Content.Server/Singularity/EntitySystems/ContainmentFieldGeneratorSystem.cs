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

        private static void BodyTypeChanged(
            EntityUid uid,
            ContainmentFieldGeneratorComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.OnAnchoredChanged();
        }
    }
}
