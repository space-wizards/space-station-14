using Content.Server.Disposal.Tube.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Disposal.Tube
{
    public sealed class DisposalTubeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalTubeComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            DisposalTubeComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.AnchoredChanged();
        }
    }
}
