using System;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Stunnable.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.DoAfter
{
    public sealed class DoAfter
    {
        public Task<DoAfterStatus> AsTask { get; }

        private TaskCompletionSource<DoAfterStatus> Tcs { get; }

        public readonly DoAfterEventArgs EventArgs;

        public TimeSpan StartTime { get; }

        public float Elapsed { get; set; }

        public EntityCoordinates UserGrid { get; }

        public EntityCoordinates TargetGrid { get; }

        public bool TookDamage { get; set; }

        public DoAfterStatus Status => AsTask.IsCompletedSuccessfully ? AsTask.Result : DoAfterStatus.Running;

        // NeedHand
        private readonly string? _activeHand;
        private readonly ItemComponent? _activeItem;

        public DoAfter(DoAfterEventArgs eventArgs, IEntityManager entityManager)
        {
            EventArgs = eventArgs;
            StartTime = IoCManager.Resolve<IGameTiming>().CurTime;

            if (eventArgs.BreakOnUserMove)
            {
                UserGrid = entityManager.GetComponent<ITransformComponent>(eventArgs.User).Coordinates;
            }

            if (eventArgs.BreakOnTargetMove)
            {
                // Target should never be null if the bool is set.
                TargetGrid = entityManager.GetComponent<ITransformComponent>(eventArgs.Target!.Value).Coordinates;
            }

            // For this we need to stay on the same hand slot and need the same item in that hand slot
            // (or if there is no item there we need to keep it free).
            if (eventArgs.NeedHand && entityManager.TryGetComponent(eventArgs.User, out HandsComponent? handsComponent))
            {
                _activeHand = handsComponent.ActiveHand;
                _activeItem = handsComponent.GetActiveHand;
            }

            Tcs = new TaskCompletionSource<DoAfterStatus>();
            AsTask = Tcs.Task;
        }

        public void Run(float frameTime, IEntityManager entityManager)
        {
            switch (Status)
            {
                case DoAfterStatus.Running:
                    break;
                case DoAfterStatus.Cancelled:
                case DoAfterStatus.Finished:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Elapsed += frameTime;

            if (IsFinished())
            {
                // Do the final checks here
                if (!TryPostCheck())
                {
                    Tcs.SetResult(DoAfterStatus.Cancelled);
                }
                else
                {
                    Tcs.SetResult(DoAfterStatus.Finished);
                }

                return;
            }

            if (IsCancelled(entityManager))
            {
                Tcs.SetResult(DoAfterStatus.Cancelled);
            }
        }

        private bool IsCancelled(IEntityManager entityManager)
        {
            if (!entityManager.EntityExists(EventArgs.User) || EventArgs.Target is {} target && !entityManager.EntityExists(target))
            {
                return true;
            }

            //https://github.com/tgstation/tgstation/blob/1aa293ea337283a0191140a878eeba319221e5df/code/__HELPERS/mobs.dm
            if (EventArgs.CancelToken.IsCancellationRequested)
            {
                return true;
            }

            // TODO :Handle inertia in space.
            if (EventArgs.BreakOnUserMove && !entityManager.GetComponent<ITransformComponent>(EventArgs.User).Coordinates.InRange(
                entityManager, UserGrid, EventArgs.MovementThreshold))
            {
                return true;
            }

            if (EventArgs.BreakOnTargetMove && !entityManager.GetComponent<ITransformComponent>(EventArgs.Target!.Value).Coordinates.InRange(
                entityManager, TargetGrid, EventArgs.MovementThreshold))
            {
                return true;
            }

            if (EventArgs.BreakOnDamage && TookDamage)
            {
                return true;
            }

            if (EventArgs.ExtraCheck != null && !EventArgs.ExtraCheck.Invoke())
            {
                return true;
            }

            if (EventArgs.BreakOnStun &&
                entityManager.TryGetComponent(EventArgs.User, out StunnableComponent? stunnableComponent) &&
                stunnableComponent.Stunned)
            {
                return true;
            }

            if (EventArgs.NeedHand)
            {
                if (!entityManager.TryGetComponent(EventArgs.User, out HandsComponent? handsComponent))
                {
                    // If we had a hand but no longer have it that's still a paddlin'
                    if (_activeHand != null)
                    {
                        return true;
                    }
                }
                else
                {
                    var currentActiveHand = handsComponent.ActiveHand;
                    if (_activeHand != currentActiveHand)
                    {
                        return true;
                    }

                    var currentItem = handsComponent.GetActiveHand;
                    if (_activeItem != currentItem)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryPostCheck()
        {
            return EventArgs.PostCheck?.Invoke() != false;
        }

        private bool IsFinished()
        {
            if (Elapsed <= EventArgs.Delay)
            {
                return false;
            }

            return true;
        }
    }
}
