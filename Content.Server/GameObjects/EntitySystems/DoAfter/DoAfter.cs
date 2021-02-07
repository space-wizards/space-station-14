#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.DoAfter
{
    public sealed class DoAfter
    {
        public Task<DoAfterStatus> AsTask { get; }

        private TaskCompletionSource<DoAfterStatus> Tcs { get;}

        public DoAfterEventArgs EventArgs;

        public TimeSpan StartTime { get; }

        public float Elapsed { get; set; }

        public EntityCoordinates UserGrid { get; }

        public EntityCoordinates TargetGrid { get; }

        private bool _tookDamage;

        public DoAfterStatus Status => AsTask.IsCompletedSuccessfully ? AsTask.Result : DoAfterStatus.Running;

        // NeedHand
        private readonly string? _activeHand;
        private readonly ItemComponent? _activeItem;

        public DoAfter(DoAfterEventArgs eventArgs)
        {
            EventArgs = eventArgs;
            StartTime = IoCManager.Resolve<IGameTiming>().CurTime;

            if (eventArgs.BreakOnUserMove)
            {
                UserGrid = eventArgs.User.Transform.Coordinates;
            }

            if (eventArgs.BreakOnTargetMove)
            {
                // Target should never be null if the bool is set.
                TargetGrid = eventArgs.Target!.Transform.Coordinates;
            }

            // For this we need to stay on the same hand slot and need the same item in that hand slot
            // (or if there is no item there we need to keep it free).
            if (eventArgs.NeedHand && eventArgs.User.TryGetComponent(out HandsComponent? handsComponent))
            {
                _activeHand = handsComponent.ActiveHand;
                _activeItem = handsComponent.GetActiveHand;
            }

            Tcs = new TaskCompletionSource<DoAfterStatus>();
            AsTask = Tcs.Task;
        }

        public void HandleDamage(DamageChangedEventArgs args)
        {
            _tookDamage = true;
        }

        public void Run(float frameTime)
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

            if (IsCancelled())
            {
                Tcs.SetResult(DoAfterStatus.Cancelled);
            }
        }

        private bool IsCancelled()
        {
            if (EventArgs.User.Deleted || EventArgs.Target?.Deleted == true)
            {
                return true;
            }

            //https://github.com/tgstation/tgstation/blob/1aa293ea337283a0191140a878eeba319221e5df/code/__HELPERS/mobs.dm
            if (EventArgs.CancelToken.IsCancellationRequested)
            {
                return true;
            }

            // TODO :Handle inertia in space.
            if (EventArgs.BreakOnUserMove && !EventArgs.User.Transform.Coordinates.InRange(
                EventArgs.User.EntityManager, UserGrid, EventArgs.MovementThreshold))
            {
                return true;
            }

            if (EventArgs.BreakOnTargetMove && !EventArgs.Target!.Transform.Coordinates.InRange(
                EventArgs.User.EntityManager, TargetGrid, EventArgs.MovementThreshold))
            {
                return true;
            }

            if (EventArgs.BreakOnDamage && _tookDamage)
            {
                return true;
            }

            if (EventArgs.ExtraCheck != null && !EventArgs.ExtraCheck.Invoke())
            {
                return true;
            }

            if (EventArgs.BreakOnStun &&
                EventArgs.User.TryGetComponent(out StunnableComponent? stunnableComponent) &&
                stunnableComponent.Stunned)
            {
                return true;
            }

            if (EventArgs.NeedHand)
            {
                if (!EventArgs.User.TryGetComponent(out HandsComponent? handsComponent))
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
