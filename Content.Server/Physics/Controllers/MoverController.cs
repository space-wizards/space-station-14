using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Physics.Controllers
{
    public sealed class MoverController : SharedMoverController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly ThrusterSystem _thruster = default!;

        private Dictionary<ShuttleComponent, List<(PilotComponent, MobMoverComponent)>> _shuttlePilots = new();

        protected override Filter GetSoundPlayers(EntityUid mover)
        {
            return Filter.Pvs(mover, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == mover);
        }

        protected override bool CanSound() => true;

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            foreach (var rider in EntityQuery<RiderComponent>())
            {

            }

            foreach (var mover in EntityQuery<ShuttleMoverComponent>())
            {
                EntityUid grid;

                if (_mapManager.IsGrid(mover.Owner))
                {
                    grid = mover.Owner;
                }
                else if (TryComp<TransformComponent>(mover.Owner, out var xform) && xform.GridUid != null)
                {
                    grid = xform.GridUid.Value;
                }
                else
                {
                    continue;
                }

                if (!TryComp<ShuttleComponent>(grid, out var shuttle)) continue;

                HandleShuttleMovement(shuttle, frameTime);
            }

            foreach (var (mover, physics, xform) in EntityQuery<MobMoverComponent, PhysicsComponent, TransformComponent>())
            {
                HandleMobMovement(mover, physics, xform, frameTime);
            }
        }

        private void HandleShuttleMovement(ShuttleComponent component, float frameTime)
        {
            throw new NotImplementedException();
        }
    }
}
