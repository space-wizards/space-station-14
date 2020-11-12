#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.Components.Buckle;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedClimbingComponent))]
    public class ClimbingComponent : SharedClimbingComponent
    {
        private bool _isClimbing;

        private bool _startedClimb;

        private CancellationTokenSource? _cancelToken;

        public override bool IsClimbing
        {
            get => _isClimbing;
            set
            {
                if (_isClimbing == value)
                    return;

                _isClimbing = value;
                Dirty();
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case BuckleMessage msg:
                    if (msg.Buckled)
                        IsClimbing = false;

                    break;
            }
        }

        /// <summary>
        /// Make the owner climb from one point to another
        /// </summary>
        private void TryMoveTo(Vector2 to)
        {
            if (Body == null)
                return;

            Body.WakeBody();
            Body.ApplyImpulse(to.Normalized * 500f);

            _startedClimb = true;
            Owner.SpawnTimer(500, () =>
            {
                if (Deleted) return;
                _startedClimb = false;
            });
            // TODO: SpawnEvent here so the climbsystem only iterates this component rather than every single one.
        }

        public async Task TryClimb(SharedClimbableComponent climbableComponent)
        {
            _cancelToken?.Cancel();
            _cancelToken = new CancellationTokenSource();

            var doAfterEventArgs = new DoAfterEventArgs(Owner, climbableComponent.ClimbDelay, _cancelToken.Token, Owner)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true
            };

            var result = await EntitySystem.Get<DoAfterSystem>().DoAfter(doAfterEventArgs);

            if (result == DoAfterStatus.Cancelled) return;

            var direction = climbableComponent.Owner.Transform.WorldPosition - Owner.Transform.WorldPosition;
            IsClimbing = true;

            TryMoveTo(direction);
        }

        public void Update()
        {
            if (!IsClimbing || Body == null || _startedClimb)
                return;

            Body.WakeBody();

            if (!IsOnClimbableThisFrame && IsClimbing)
                IsClimbing = false;

            IsOnClimbableThisFrame = false;
        }

        public override ComponentState GetComponentState()
        {
            return new ClimbModeComponentState(_isClimbing);
        }
    }
}
