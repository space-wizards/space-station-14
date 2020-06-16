using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    public class BuckleableComponent : Component, IActionBlocker, IInteractHand, IMoveSpeedModifier
    {
        public override string Name => "Buckleable";

        private bool _buckled = false;

        [ViewVariables] public bool Buckled => _buckled;
        public float WalkSpeedModifier => 0;
        public float SprintSpeedModifier => 0;

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            _buckled = false;
            return true;
        }

        public bool CanMove() => !Buckled;
    }
}
