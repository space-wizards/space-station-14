using Robust.Shared.GameObjects;

namespace Content.Server.Disposal.Mailing
{
    public sealed class DisposalMailingUnitSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalMailingUnitComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<DisposalMailingUnitComponent, PhysicsBodyTypeChangedEvent>();
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            DisposalMailingUnitComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.UpdateVisualState();
        }
    }
}
