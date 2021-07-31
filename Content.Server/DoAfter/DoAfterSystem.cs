using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.DoAfter
{
    [UsedImplicitly]
    public sealed class DoAfterSystem : EntitySystem
    {
        // We cache these lists as to not allocate them every update tick...
        private readonly List<DoAfter> _cancelled = new();
        private readonly List<DoAfter> _finished = new();

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in ComponentManager.EntityQuery<DoAfterComponent>(true))
            {
                foreach (var doAfter in comp.DoAfters.ToArray())
                {
                    doAfter.Run(frameTime);

                    switch (doAfter.Status)
                    {
                        case DoAfterStatus.Running:
                            break;
                        case DoAfterStatus.Cancelled:
                            _cancelled.Add(doAfter);
                            break;
                        case DoAfterStatus.Finished:
                            _finished.Add(doAfter);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                foreach (var doAfter in _cancelled)
                {
                    comp.Cancelled(doAfter);

                    if(!doAfter.EventArgs.User.Deleted && doAfter.EventArgs.UserCancelledEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.User.Uid, doAfter.EventArgs.UserCancelledEvent, false);

                    if(doAfter.EventArgs.Target is { Deleted: false } && doAfter.EventArgs.TargetCancelledEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.Target.Uid, doAfter.EventArgs.TargetCancelledEvent, false);

                    if(doAfter.EventArgs.BroadcastCancelledEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.BroadcastCancelledEvent);
                }

                foreach (var doAfter in _finished)
                {
                    comp.Finished(doAfter);

                    if(!doAfter.EventArgs.User.Deleted && doAfter.EventArgs.UserFinishedEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.User.Uid, doAfter.EventArgs.UserFinishedEvent, false);

                    if(doAfter.EventArgs.Target is { Deleted: false } && doAfter.EventArgs.TargetFinishedEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.Target.Uid, doAfter.EventArgs.TargetFinishedEvent, false);

                    if(doAfter.EventArgs.BroadcastFinishedEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.BroadcastFinishedEvent);
                }

                // Clean the shared lists at the end, ensuring they'll be clean for the next time we need them.
                _cancelled.Clear();
                _finished.Clear();
            }
        }

        /// <summary>
        ///     Tasks that are delayed until the specified time has passed
        ///     These can be potentially cancelled by the user moving or when other things happen.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public async Task<DoAfterStatus> WaitDoAfter(DoAfterEventArgs eventArgs)
        {
            var doAfter = CreateDoAfter(eventArgs);

            await doAfter.AsTask;

            return doAfter.Status;
        }

        /// <summary>
        ///     Creates a DoAfter without waiting for it to finish. You can use events with this.
        ///     These can be potentially cancelled by the user moving or when other things happen.
        /// </summary>
        /// <param name="eventArgs"></param>
        public void DoAfter(DoAfterEventArgs eventArgs)
        {
            CreateDoAfter(eventArgs);
        }

        private DoAfter CreateDoAfter(DoAfterEventArgs eventArgs)
        {
            // Setup
            var doAfter = new DoAfter(eventArgs);
            // Caller's gonna be responsible for this I guess
            var doAfterComponent = eventArgs.User.GetComponent<DoAfterComponent>();
            doAfterComponent.Add(doAfter);
            return doAfter;
        }
    }

    public enum DoAfterStatus
    {
        Running,
        Cancelled,
        Finished,
    }
}
