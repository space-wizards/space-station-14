using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public GhostGui? Gui { get; set; }
        public bool IsAttached { get; set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (Owner == _playerManager.LocalPlayer!.ControlledEntity)
            {
                Gui?.Update();
            }
        }
    }
}
