#nullable enable
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
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in ComponentManager.EntityQuery<DoAfterComponent>(true))
            {
                var cancelled = new List<DoAfter>(0);
                var finished = new List<DoAfter>(0);

                foreach (var doAfter in comp.DoAfters.ToArray())
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

            await doAfter.AsTask;

            return doAfter.Status;
        }
    }

    public enum DoAfterStatus
    {
        Running,
        Cancelled,
        Finished,
    }
}
