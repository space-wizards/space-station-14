using Content.Client.Ghost.UI;
using Content.Shared.Ghost;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Ghost
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGhostComponent))]
    public sealed class GhostComponent : SharedGhostComponent
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public GhostGui? Gui { get; set; }
        public bool IsAttached { get; set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not GhostComponentState)
            {
                return;
            }

            if (Owner == _playerManager.LocalPlayer?.ControlledEntity)
            {
                Gui?.Update();
            }
        }
    }
}
