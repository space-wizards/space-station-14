using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameStates;

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
            SubscribeLocalEvent<DoAfterComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<DoAfterComponent, MobStateChangedEvent>(OnStateChanged);
            SubscribeLocalEvent<DoAfterComponent, ComponentGetState>(OnDoAfterGetState);
        }

        public void Add(DoAfterComponent component, DoAfter doAfter)
        {
            component.DoAfters.Add(doAfter, component.RunningIndex);
            EnsureComp<ActiveDoAfterComponent>(component.Owner);
            component.RunningIndex++;
            Dirty(component);
        }

        public void Cancelled(DoAfterComponent component, DoAfter doAfter)
        {
            if (!component.DoAfters.TryGetValue(doAfter, out var index))
                return;

            component.DoAfters.Remove(doAfter);

            if (component.DoAfters.Count == 0)
            {
                RemComp<ActiveDoAfterComponent>(component.Owner);
            }

            RaiseNetworkEvent(new CancelledDoAfterMessage(component.Owner, index));
        }

        /// <summary>
        ///     Call when the particular DoAfter is finished.
        ///     Client should be tracking this independently.
        /// </summary>
        public void Finished(DoAfterComponent component, DoAfter doAfter)
        {
            if (!component.DoAfters.ContainsKey(doAfter))
                return;

            component.DoAfters.Remove(doAfter);

            if (component.DoAfters.Count == 0)
            {
                RemComp<ActiveDoAfterComponent>(component.Owner);
            }
        }

        private void OnDoAfterGetState(EntityUid uid, DoAfterComponent component, ref ComponentGetState args)
        {
            var toAdd = new List<ClientDoAfter>(component.DoAfters.Count);

            foreach (var (doAfter, _) in component.DoAfters)
            {
                // THE ALMIGHTY PYRAMID
                var clientDoAfter = new ClientDoAfter(
                    component.DoAfters[doAfter],
                    doAfter.UserGrid,
                    doAfter.TargetGrid,
                    doAfter.StartTime,
                    doAfter.EventArgs.Delay,
                    doAfter.EventArgs.BreakOnUserMove,
                    doAfter.EventArgs.BreakOnTargetMove,
                    doAfter.EventArgs.MovementThreshold,
                    doAfter.EventArgs.DamageThreshold,
                    doAfter.EventArgs.Target);

                toAdd.Add(clientDoAfter);
            }

            args.State = new DoAfterComponentState(toAdd);
        }

        private void OnStateChanged(EntityUid uid, DoAfterComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Alive)
                return;

            foreach (var (doAfter, _) in component.DoAfters)
            {
                doAfter.Cancel();
            }
        }

        /// <summary>
        /// Cancels DoAfter if it breaks on damage and it meets the threshold
        /// </summary>
        /// <param name="_">
        /// The EntityUID of the user
        /// </param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        public void OnDamage(EntityUid _, DoAfterComponent component, DamageChangedEvent args)
        {
            if (!args.InterruptsDoAfters || !args.DamageIncreased || args.DamageDelta == null)
                return;

            foreach (var (doAfter, _) in component.DoAfters)
            {
                if (doAfter.EventArgs.BreakOnDamage && args.DamageDelta?.Total.Float() > doAfter.EventArgs.DamageThreshold)
                {
                    doAfter.Cancel();
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (_, comp) in EntityManager.EntityQuery<ActiveDoAfterComponent, DoAfterComponent>())
            {
                foreach (var (doAfter, _) in comp.DoAfters.ToArray())
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
                    Cancelled(comp, doAfter);

                    if(EntityManager.EntityExists(doAfter.EventArgs.User) && doAfter.EventArgs.UserCancelledEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.User, doAfter.EventArgs.UserCancelledEvent, false);

                    if (doAfter.EventArgs.Used is {} used && EntityManager.EntityExists(used) && doAfter.EventArgs.UsedCancelledEvent != null)
                        RaiseLocalEvent(used, doAfter.EventArgs.UsedCancelledEvent);

                    if(doAfter.EventArgs.Target is {} target && EntityManager.EntityExists(target) && doAfter.EventArgs.TargetCancelledEvent != null)
                        RaiseLocalEvent(target, doAfter.EventArgs.TargetCancelledEvent, false);

                    if(doAfter.EventArgs.BroadcastCancelledEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.BroadcastCancelledEvent);
                }

                while (_finished.TryDequeue(out var doAfter))
                {
                    Finished(comp, doAfter);

                    if(EntityManager.EntityExists(doAfter.EventArgs.User) && doAfter.EventArgs.UserFinishedEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.User, doAfter.EventArgs.UserFinishedEvent, false);

                    if(doAfter.EventArgs.Used is {} used && EntityManager.EntityExists(used) && doAfter.EventArgs.UsedFinishedEvent != null)
                        RaiseLocalEvent(used, doAfter.EventArgs.UsedFinishedEvent);

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
            var doAfterComponent = Comp<DoAfterComponent>(eventArgs.User);
            Add(doAfterComponent, doAfter);
            return doAfter;
        }
    }

    public enum DoAfterStatus : byte
    {
        Running,
        Cancelled,
        Finished,
    }
}
