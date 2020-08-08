#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class DoAfter
    {
        public Task<DoAfterStatus> AsTask { get; }
        
        private TaskCompletionSource<DoAfterStatus> Tcs { get;}
        
        public DoAfterEventArgs EventArgs;
        
        public TimeSpan StartTime { get; }
        
        public float Elapsed { get; set; }
        
        public GridCoordinates UserGrid { get; }
        
        public GridCoordinates TargetGrid { get; }

        private bool _tookDamage;

        public DoAfterStatus Status => AsTask.IsCompletedSuccessfully ? AsTask.Result : DoAfterStatus.Running;
        
        public DoAfter(DoAfterEventArgs eventArgs)
        {
            EventArgs = eventArgs;
            StartTime = IoCManager.Resolve<IGameTiming>().CurTime;

            if (eventArgs.BreakOnUserMove)
            {
                UserGrid = eventArgs.User.Transform.GridPosition;
            }

            if (eventArgs.BreakOnTargetMove)
            {
                TargetGrid = eventArgs.Target.Transform.GridPosition;
            }
            
            Tcs = new TaskCompletionSource<DoAfterStatus>();
            AsTask = Tcs.Task;
        }

        public void HandleDamage(object? sender, DamageEventArgs eventArgs)
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
                Tcs.SetResult(DoAfterStatus.Finished);
                return;
            }
            
            if (IsCancelled())
            {
                Tcs.SetResult(DoAfterStatus.Cancelled);
            }
        }

        private bool IsCancelled()
        {
            //https://github.com/tgstation/tgstation/blob/1aa293ea337283a0191140a878eeba319221e5df/code/__HELPERS/mobs.dm
            if (EventArgs.CancelToken.IsCancellationRequested)
            {
                return true;
            }
            
            // TODO :Handle inertia in space.
            if (EventArgs.BreakOnUserMove && EventArgs.User.Transform.GridPosition != UserGrid)
            {
                return true;
            }
            
            if (EventArgs.BreakOnTargetMove && EventArgs.Target.Transform.GridPosition != TargetGrid)
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
                EventArgs.User.TryGetComponent(out StunnableComponent stunnableComponent) &&
                stunnableComponent.Stunned)
            {
                return true;
            }

            // I didn't fully understand what this was doing in the original do_after code, if it's checking for any hand free?
            if (EventArgs.NeedHand)
            {
                // TODO:
            }

            return false;
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