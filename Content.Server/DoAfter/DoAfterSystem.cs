using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.DoAfter
{
    [UsedImplicitly]
    public sealed class DoAfterSystem : EntitySystem
    {
        // We cache these lists as to not allocate them every update tick...
        private readonly Queue<DoAfter> _cancelled = new();
        private readonly Queue<DoAfter> _finished = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DoAfterComponent, DamageChangedEvent>(HandleDamage);
            SubscribeLocalEvent<DoAfterComponent, MobStateChangedEvent>(HandleStateChanged);
        }

        private void HandleStateChanged(EntityUid uid, DoAfterComponent component, MobStateChangedEvent args)
        {
            if (!args.CurrentMobState.IsIncapacitated())
                return;

            foreach (var doAfter in component.DoAfters)
            {
                doAfter.Cancel();
            }
        }

        public void HandleDamage(EntityUid _, DoAfterComponent component, DamageChangedEvent args)
        {
            if (!args.InterruptsDoAfters || !args.DamageIncreased)
                return;

            foreach (var doAfter in component.DoAfters)
            {
                if (doAfter.EventArgs.BreakOnDamage)
                {
                    doAfter.Cancel();
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in EntityManager.EntityQuery<DoAfterComponent>())
            {
                foreach (var doAfter in comp.DoAfters.ToArray())
                {
                    doAfter.Run(frameTime, EntityManager);

                    switch (doAfter.Status)
                    {
                        case DoAfterStatus.Running:
                            break;
                        case DoAfterStatus.Cancelled:
                            _cancelled.Enqueue(doAfter);
                            break;
                        case DoAfterStatus.Finished:
                            _finished.Enqueue(doAfter);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                while (_cancelled.TryDequeue(out var doAfter))
                {
                    comp.Cancelled(doAfter);

                    if(EntityManager.EntityExists(doAfter.EventArgs.User) && doAfter.EventArgs.UserCancelledEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.User, doAfter.EventArgs.UserCancelledEvent, false);

                    if(doAfter.EventArgs.Target is {} target && EntityManager.EntityExists(target) && doAfter.EventArgs.TargetCancelledEvent != null)
                        RaiseLocalEvent(target, doAfter.EventArgs.TargetCancelledEvent, false);

                    if(doAfter.EventArgs.BroadcastCancelledEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.BroadcastCancelledEvent);
                }

                while (_finished.TryDequeue(out var doAfter))
                {
                    comp.Finished(doAfter);

                    if(EntityManager.EntityExists(doAfter.EventArgs.User) && doAfter.EventArgs.UserFinishedEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.User, doAfter.EventArgs.UserFinishedEvent, false);

                    if(doAfter.EventArgs.Target is {} target && EntityManager.EntityExists(target) && doAfter.EventArgs.TargetFinishedEvent != null)
                        RaiseLocalEvent(target, doAfter.EventArgs.TargetFinishedEvent, false);

                    if(doAfter.EventArgs.BroadcastFinishedEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.BroadcastFinishedEvent);
                }
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
            var doAfter = new DoAfter(eventArgs, EntityManager);
            // Caller's gonna be responsible for this I guess
            var doAfterComponent = EntityManager.GetComponent<DoAfterComponent>(eventArgs.User);
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
