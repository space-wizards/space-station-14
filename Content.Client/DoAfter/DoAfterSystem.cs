using System.Linq;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.DoAfter
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

        private EntityUid? _attachedEntity;

        public override void Initialize()
        {
            base.Initialize();
            UpdatesOutsidePrediction = true;
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
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
            if (_attachedEntity is not {Valid: true} entity || Deleted(entity))
                return;

            var viewbox = _eyeManager.GetWorldViewport().Enlarged(2.0f);
            var entXform = Transform(entity);
            var playerPos = entXform.MapPosition;

            foreach (var (comp, xform) in EntityManager.EntityQuery<DoAfterComponent, TransformComponent>(true))
            {
                var doAfters = comp.DoAfters.ToList();
                var compPos = xform.MapPosition;

                if (doAfters.Count == 0 ||
                    compPos.MapId != entXform.MapID ||
                    !viewbox.Contains(compPos.Position))
                {
                    comp.Disable();
                    continue;
                }

                var range = (compPos.Position - playerPos.Position).Length + 0.01f;

                if (comp.Owner != _attachedEntity &&
                    !ExamineSystemShared.InRangeUnOccluded(
                        playerPos,
                        compPos, range,
                        ent => ent == comp.Owner || ent == _attachedEntity))
                {
                    comp.Disable();
                    continue;
                }

                comp.Enable();

                var userGrid = xform.Coordinates;

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
                        if (!EntityManager.Deleted(doAfter.Target) &&
                            !Transform(doAfter.Target.Value).Coordinates.InRange(EntityManager, doAfter.TargetGrid,
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
