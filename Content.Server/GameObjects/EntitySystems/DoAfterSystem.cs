#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class DoAfterSystem : EntitySystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        private TypeEntityQuery _entityQuery = new TypeEntityQuery(typeof(DoAfterComponent));

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            
            foreach (var entity in EntityManager.GetEntities(_entityQuery))
            {
                if (_pauseManager.IsGridPaused(entity.Transform.GridID)) continue;
                
                var comp = entity.GetComponent<DoAfterComponent>();
                var cancelled = new List<DoAfter>(0);
                var finished = new List<DoAfter>(0);

                foreach (var doAfter in comp.DoAfters)
                {
                    doAfter.Run(frameTime);
                    
                    switch (doAfter.Status)
                    {
                        case DoAfterStatus.Running:
                            break;
                        case DoAfterStatus.Cancelled:
                            cancelled.Add(doAfter);
                            break;
                        case DoAfterStatus.Finished:
                            finished.Add(doAfter);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                foreach (var doAfter in cancelled)
                {
                    comp.Cancelled(doAfter);
                }

                foreach (var doAfter in finished)
                {
                    comp.Finished(doAfter);
                }

                finished.Clear();
            }
        }
        
        /// <summary>
        ///     Tasks that are delayed until the specified time has passed
        ///     These can be potentially cancelled by the user moving or when other things happen.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public async Task<DoAfterStatus> DoAfter(DoAfterEventArgs eventArgs)
        {
            // Setup
            var doAfter = new DoAfter(eventArgs);
            // Caller's gonna be responsible for this I guess
            var doAfterComponent = eventArgs.User.GetComponent<DoAfterComponent>();
            doAfterComponent.Add(doAfter);
            DamageableComponent? damageableComponent = null;
            
            // TODO: If the component's deleted this may not get unsubscribed?
            if (eventArgs.BreakOnDamage && eventArgs.User.TryGetComponent(out damageableComponent))
            {
                damageableComponent.Damaged += doAfter.HandleDamage;
            }

            await doAfter.AsTask;
            
            if (damageableComponent != null)
            {
                damageableComponent.Damaged -= doAfter.HandleDamage;
            }
            
            return doAfter.Status;
        }
    }

    public sealed class DoAfterEventArgs
    {
        /// <summary>
        ///     The entity invoking do_after
        /// </summary>
        public IEntity User { get; }
        
        /// <summary>
        ///     How long does the do_after require to complete
        /// </summary>
        public float Delay { get; }

        /// <summary>
        ///     Applicable target (if relevant)
        /// </summary>
        public IEntity Target { get; }
        
        /// <summary>
        ///     Manually cancel the do_after so it no longer runs
        /// </summary>
        public CancellationToken CancelToken { get; }
        
        // Break the chains
        public bool NeedHand { get; }
        
        /// <summary>
        ///     If do_after stops when the user moves
        /// </summary>
        public bool BreakOnUserMove { get; }
        
        /// <summary>
        ///     If do_after stops when the target moves (if there is a target)
        /// </summary>
        public bool BreakOnTargetMove { get; }
        public bool BreakOnDamage { get; }
        public bool BreakOnStun { get; }
        
        /// <summary>
        ///     Additional conditions that need to be met. Return false to cancel.
        /// </summary>
        public Func<bool> ExtraCheck { get; }
        
        public DoAfterEventArgs(
            IEntity user, 
            float delay,
            CancellationToken cancelToken,
            IEntity target = null,
            bool needHand = true,
            bool breakOnUserMove = true,
            bool breakOnTargetMove = true,
            bool breakOnDamage = true,
            bool breakOnStun = true,
            Func<bool> extraCheck = null
            )
        {
            User = user;
            Delay = delay;
            CancelToken = cancelToken;
            Target = target;
            NeedHand = needHand;
            BreakOnUserMove = breakOnUserMove;
            BreakOnTargetMove = breakOnTargetMove;
            BreakOnDamage = breakOnDamage;
            BreakOnStun = breakOnStun;
            ExtraCheck = extraCheck;
            
            if (Target == null)
            {
                BreakOnTargetMove = false;
            }
        }
    }

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

        public void HandleDamage(object sender, DamageEventArgs eventArgs)
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
                // Stunned or paralyzed on tgstation
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

    public enum DoAfterStatus
    {
        Running,
        Cancelled,
        Finished,
    }
}