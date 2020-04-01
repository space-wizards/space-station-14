using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client.State
{
    public abstract class StateBase : Robust.Client.State.State
    {
#pragma warning disable 649
        [Dependency] private readonly IClientEntityManager _entityManager;
        [Dependency] private readonly IComponentManager _componentManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IPlacementManager _placementManager;
#pragma warning restore 649

        public override void Update(FrameEventArgs e)
        {
            _componentManager.CullRemovedComponents();
            _entityManager.Update(e.DeltaSeconds);
            _playerManager.Update(e.DeltaSeconds);
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            _placementManager.FrameUpdate(e);
            _entityManager.FrameUpdate(e.DeltaSeconds);
        }
    }
}
