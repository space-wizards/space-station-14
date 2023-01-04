using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.DoAfter;

    /// <summary>
    /// Handles events that need to happen after a certain amount of time where the event could be cancelled by factors
    /// such as moving.
    /// </summary>
    [UsedImplicitly]
    public sealed class DoAfterSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IOverlayManager _overlay = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        /// <summary>
        ///     We'll use an excess time so stuff like finishing effects can show.
        /// </summary>
        public const float ExcessTime = 0.5f;

        public override void Initialize()
        {
            base.Initialize();
            UpdatesOutsidePrediction = true;
            SubscribeNetworkEvent<CancelledDoAfterMessage>(OnCancelledDoAfter);
            SubscribeLocalEvent<DoAfterComponent, ComponentHandleState>(OnDoAfterHandleState);
            _overlay.AddOverlay(
                new DoAfterOverlay(
                    EntityManager,
                    _prototype));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlay.RemoveOverlay<DoAfterOverlay>();
        }

        private void OnDoAfterHandleState(EntityUid uid, DoAfterComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not DoAfterComponentState state)
                return;

            var toRemove = new RemQueue<Shared.DoAfter.DoAfter>();

            foreach (var (id, doAfter) in component.DoAfters)
            {
                var found = false;

                foreach (var clientdoAfter in state.DoAfters)
                {
                    if (clientdoAfter.ID == id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    toRemove.Add(doAfter);
                }
            }

            foreach (var doAfter in toRemove)
            {
                Remove(component, doAfter);
            }

            foreach (var doAfter in state.DoAfters)
            {
                if (component.DoAfters.ContainsValue(doAfter))
                    continue;

                component.DoAfters.Add(doAfter.ID, doAfter);
            }
        }

        private void OnCancelledDoAfter(CancelledDoAfterMessage ev)
        {
            if (!TryComp<DoAfterComponent>(ev.Uid, out var doAfter))
                return;

            Cancel(doAfter, ev.ID);
        }

        /// <summary>
        ///     Remove a DoAfter without showing a cancellation graphic.
        /// </summary>
        public void Remove(DoAfterComponent component, Shared.DoAfter.DoAfter doAfter)
        {
            component.DoAfters.Remove(doAfter.ID);

            var found = false;

            component.CancelledDoAfters.Remove(doAfter.ID);

            if (!found)
                component.DoAfters.Remove(doAfter.ID);
        }

        /// <summary>
        ///     Mark a DoAfter as cancelled and show a cancellation graphic.
        /// </summary>
        ///     Actual removal is handled by DoAfterEntitySystem.
        public void Cancel(DoAfterComponent component, byte id)
        {
            if (component.CancelledDoAfters.ContainsKey(id))
                return;

            if (!component.DoAfters.ContainsKey(id))
                return;

            var doAfterMessage = component.DoAfters[id];
            doAfterMessage.Cancelled = true;
            component.CancelledDoAfters.Add(id, doAfterMessage);
        }

        // TODO separate DoAfter & ActiveDoAfter components for the entity query.
        public override void Update(float frameTime)
        {
            if (!_gameTiming.IsFirstTimePredicted)
                return;

            var playerEntity = _player.LocalPlayer?.ControlledEntity;

            foreach (var (comp, xform) in EntityQuery<DoAfterComponent, TransformComponent>())
            {
                var doAfters = comp.DoAfters;

                if (doAfters.Count == 0)
                {
                    continue;
                }

                var userGrid = xform.Coordinates;
                var toRemove = new RemQueue<Shared.DoAfter.DoAfter>();

                // Check cancellations / finishes
                foreach (var (id, doAfter) in doAfters)
                {
                    // If we've passed the final time (after the excess to show completion graphic) then remove.
                    if ((doAfter.Elapsed.TotalSeconds + doAfter.CancelledElapsed.TotalSeconds) > doAfter.Delay + ExcessTime)
                    {
                        toRemove.Add(doAfter);
                        continue;
                    }

                    if (doAfter.Cancelled)
                    {
                        doAfter.CancelledElapsed = _gameTiming.CurTime - doAfter.StartTime;
                        continue;
                    }

                    doAfter.Elapsed = _gameTiming.CurTime - doAfter.StartTime;

                    // Well we finished so don't try to predict cancels.
                    if (doAfter.Elapsed.TotalSeconds > doAfter.Delay)
                    {
                        continue;
                    }

                    // Predictions
                    if (comp.Owner != playerEntity)
                        continue;

                    // TODO: Add these back in when I work out some system for changing the accumulation rate
                    // based on ping. Right now these would show as cancelled near completion if we moved at the end
                    // despite succeeding.
                    continue;

                    /*if (doAfter.BreakOnUserMove)
                    {
                        if (!userGrid.InRange(EntityManager, doAfter.UserGrid, doAfter.MovementThreshold))
                        {
                            Cancel(comp, id);
                            continue;
                        }
                    }

                    if (doAfter.BreakOnTargetMove)
                    {
                        if (!EntityManager.Deleted(doAfter.Target) &&
                            !Transform(doAfter.Target.Value).Coordinates.InRange(EntityManager, doAfter.TargetGrid,
                                doAfter.MovementThreshold))
                        {
                            Cancel(comp, id);
                            continue;
                        }
                    }*/
                }

                foreach (var doAfter in toRemove)
                {
                    Remove(comp, doAfter);
                }

                // Remove cancelled DoAfters after ExcessTime has elapsed
                var toRemoveCancelled = new List<Shared.DoAfter.DoAfter>();

                foreach (var (_, doAfter) in comp.CancelledDoAfters)
                {
                    if ((float)doAfter.CancelledElapsed.TotalSeconds > ExcessTime)
                    {
                        toRemoveCancelled.Add(doAfter);
                    }
                }

                foreach (var doAfter in toRemoveCancelled)
                {
                    Remove(comp, doAfter);
                }
            }
        }
    }
