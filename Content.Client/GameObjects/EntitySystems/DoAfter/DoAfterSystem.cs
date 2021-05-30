using System.Linq;
using Content.Client.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client.GameObjects.EntitySystems.DoAfter
{
    /// <summary>
    /// Handles events that need to happen after a certain amount of time where the event could be cancelled by factors
    /// such as moving.
    /// </summary>
    [UsedImplicitly]
    public sealed class DoAfterSystem : EntitySystem
    {
    /*
     * How this is currently setup (client-side):
     * DoAfterGui handles the actual bars displayed above heads. It also uses FrameUpdate to flash cancellations
     * DoAfterEntitySystem handles checking predictions every tick as well as removing / cancelling DoAfters due to time elapsed.
     * DoAfterComponent handles network messages inbound as well as storing the DoAfter data.
     *     It'll also handle overall cleanup when one is removed (i.e. removing it from DoAfterGui).
    */
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        /// <summary>
        ///     We'll use an excess time so stuff like finishing effects can show.
        /// </summary>
        public const float ExcessTime = 0.5f;

        private IEntity? _attachedEntity;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeLocalEvent<PlayerAttachSysMessage>();
        }

        private void HandlePlayerAttached(PlayerAttachSysMessage message)
        {
            _attachedEntity = message.AttachedEntity;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var currentTime = _gameTiming.CurTime;

            // Can't see any I guess?
            if (_attachedEntity == null || _attachedEntity.Deleted)
                return;

            var viewbox = _eyeManager.GetWorldViewport().Enlarged(2.0f);

            foreach (var comp in ComponentManager.EntityQuery<DoAfterComponent>(true))
            {
                var doAfters = comp.DoAfters.ToList();
                var compPos = comp.Owner.Transform.WorldPosition;

                if (doAfters.Count == 0 ||
                    comp.Owner.Transform.MapID != _attachedEntity.Transform.MapID ||
                    !viewbox.Contains(compPos))
                {
                    comp.Disable();
                    continue;
                }

                var range = (compPos - _attachedEntity.Transform.WorldPosition).Length +
                            0.01f;

                if (comp.Owner != _attachedEntity &&
                    !ExamineSystemShared.InRangeUnOccluded(
                        _attachedEntity.Transform.MapPosition,
                        comp.Owner.Transform.MapPosition, range,
                        entity => entity == comp.Owner || entity == _attachedEntity))
                {
                    comp.Disable();
                    continue;
                }

                comp.Enable();

                var userGrid = comp.Owner.Transform.Coordinates;

                // Check cancellations / finishes
                foreach (var (id, doAfter) in doAfters)
                {
                    var elapsedTime = (currentTime - doAfter.StartTime).TotalSeconds;

                    // If we've passed the final time (after the excess to show completion graphic) then remove.
                    if (elapsedTime > doAfter.Delay + ExcessTime)
                    {
                        comp.Remove(doAfter);
                        continue;
                    }

                    // Don't predict cancellation if it's already finished.
                    if (elapsedTime > doAfter.Delay)
                    {
                        continue;
                    }

                    // Predictions
                    if (doAfter.BreakOnUserMove)
                    {
                        if (!userGrid.InRange(EntityManager, doAfter.UserGrid, doAfter.MovementThreshold))
                        {
                            comp.Cancel(id, currentTime);
                            continue;
                        }
                    }

                    if (doAfter.BreakOnTargetMove)
                    {
                        if (EntityManager.TryGetEntity(doAfter.TargetUid, out var targetEntity) &&
                            !targetEntity.Transform.Coordinates.InRange(EntityManager, doAfter.TargetGrid,
                                doAfter.MovementThreshold))
                        {
                            comp.Cancel(id, currentTime);
                            continue;
                        }
                    }
                }

                var count = comp.CancelledDoAfters.Count;
                // Remove cancelled DoAfters after ExcessTime has elapsed
                for (var i = count - 1; i >= 0; i--)
                {
                    var cancelled = comp.CancelledDoAfters[i];
                    if ((currentTime - cancelled.CancelTime).TotalSeconds > ExcessTime)
                    {
                        comp.Remove(cancelled.Message);
                    }
                }
            }
        }
    }
}
