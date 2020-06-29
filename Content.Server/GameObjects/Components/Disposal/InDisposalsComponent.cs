using Content.Server.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
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
        public IDisposalTubeComponent PreviousTube { get; private set; }

        [CanBeNull, ViewVariables]
        public IDisposalTubeComponent CurrentTube { get; set; }

        [CanBeNull, ViewVariables]
        public IDisposalTubeComponent NextTube { get; private set; }

        /// <summary>
        ///     The total amount of time that it will take for this entity to
        ///     be pushed to the next tube
        /// </summary>
        [ViewVariables]
        public float StartingTime { get; set; }

        /// <summary>
        ///     Time left until the entity is pushed to the next tube
        /// </summary>
        [ViewVariables]
        public float TimeLeft { get; set; }

        public void EnterTube(IDisposalTubeComponent tube)
        {
            if (CurrentTube != null)
            {
                PreviousTube = CurrentTube;
            }

            Owner.Transform.GridPosition = tube.Owner.Transform.GridPosition;
            CurrentTube = tube;
            NextTube = tube.NextTube(this);
            StartingTime = 1;
            TimeLeft = 1;
        }

        public void ExitDisposals()
        {
            PreviousTube = null;
            CurrentTube = null;
            NextTube = null;
            StartingTime = 0;
            TimeLeft = 0;
            Owner.Transform.DetachParent();

            _componentManager.RemoveComponent(Owner.Uid, this);
        }

        public void Update(float frameTime)
        {
            CurrentTube?.Update(frameTime, Owner);
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
