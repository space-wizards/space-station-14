using Content.Server.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class InDisposalsComponent : Component, IActionBlocker
    {
        public override string Name => "InDisposals";

#pragma warning disable 649
        [Dependency] private readonly IComponentManager _componentManager;
#pragma warning restore 649

        [CanBeNull, ViewVariables]
        private IDisposalTubeComponent DisposalTube { get; set; }

        [CanBeNull, ViewVariables]
        public IDisposalTubeComponent PreviousTube { get; private set; }

        /// <summary>
        /// The total amount of time that it will take for this entity to
        /// be pushed to the next tube
        /// </summary>
        [ViewVariables]
        public float StartingTime { get; set; }

        /// <summary>
        /// Time left until the entity is pushed to the next tube
        /// </summary>
        [ViewVariables]
        public float TimeLeft { get; set; }

        public void EnterTube(IDisposalTubeComponent tube)
        {
            if (DisposalTube != null)
            {
                PreviousTube = DisposalTube;
            }

            Owner.Transform.GridPosition = tube.Owner.Transform.GridPosition;
            DisposalTube = tube;
            StartingTime = 0.1f;
            TimeLeft = 0.1f;
        }

        public void ExitDisposals()
        {
            DisposalTube = null;
            PreviousTube = null;
            StartingTime = 0;
            TimeLeft = 0;
            Owner.Transform.DetachParent();

            _componentManager.RemoveComponent(Owner.Uid, this);
        }

        public void Update(float frameTime)
        {
            DisposalTube?.Update(frameTime, Owner);
        }

        public override void OnRemove()
        {
            base.OnRemove();
            ExitDisposals();
        }

        bool IActionBlocker.CanMove()
        {
            return false;
        }

        bool IActionBlocker.CanInteract()
        {
            return false;
        }

        bool IActionBlocker.CanUse()
        {
            return false;
        }

        bool IActionBlocker.CanThrow()
        {
            return false;
        }

        bool IActionBlocker.CanDrop()
        {
            return false;
        }

        bool IActionBlocker.CanPickup()
        {
            return false;
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return false;
        }
    }
}
