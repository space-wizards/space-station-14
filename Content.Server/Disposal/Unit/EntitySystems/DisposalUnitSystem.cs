using Content.Server.Disposal.Unit.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    public sealed class DisposalUnitSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalUnitComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
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
