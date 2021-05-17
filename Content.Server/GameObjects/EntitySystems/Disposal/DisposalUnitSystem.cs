using Content.Server.GameObjects.Components.Disposal;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Disposal
{
    public sealed class DisposalUnitSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalUnitComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<DisposalUnitComponent, PhysicsBodyTypeChangedEvent>();
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            DisposalUnitComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.UpdateVisualState();
        }
    }
}
