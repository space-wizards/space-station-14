using Content.Server.Disposal.Tube.Components;
using Content.Shared.Movement;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Disposal.Tube
{
    public sealed class DisposalTubeSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalTubeComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
            SubscribeLocalEvent<DisposalTubeComponent, RelayMovementEntityEvent>(OnRelayMovement);
        }

        private void OnRelayMovement(EntityUid uid, DisposalTubeComponent component, RelayMovementEntityEvent args)
        {
            if (_gameTiming.CurTime < component.LastClang + DisposalTubeComponent.ClangDelay)
            {
                return;
            }

            var entity = EntityManager.GetEntity(uid);
            component.LastClang = _gameTiming.CurTime;
            SoundSystem.Play(Filter.Pvs(entity), component.ClangSound.GetSound(), entity);
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
