#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

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
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        /// <summary>
        ///     We'll use an excess time so stuff like finishing effects can show.
        /// </summary>
        public const float ExcessTime = 0.5f;

        // Each component in range will have its own vBox which we need to keep track of so if they go out of range or
        // come into range it needs altering
        private readonly HashSet<DoAfterComponent> _knownComponents = new HashSet<DoAfterComponent>();

        private IEntity? _attachedEntity;

        public override void Initialize()
        {
            base.Initialize();
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
            var foundComps = new HashSet<DoAfterComponent>();

            // Can't see any I guess?
            if (_attachedEntity == null || _attachedEntity.Deleted)
                return;

            foreach (var comp in ComponentManager.EntityQuery<DoAfterComponent>())
            {
                if (!_knownComponents.Contains(comp))
                {
                    _knownComponents.Add(comp);
                }

                var doAfters = comp.DoAfters.ToList();

                if (doAfters.Count == 0)
                {
                    if (comp.Gui != null)
                        comp.Gui.FirstDraw = true;

                    continue;
                }

                var range = (comp.Owner.Transform.WorldPosition - _attachedEntity.Transform.WorldPosition).Length + 0.01f;

                if (comp.Owner != _attachedEntity && !ExamineSystemShared.InRangeUnOccluded(
                    _attachedEntity.Transform.MapPosition,
                    comp.Owner.Transform.MapPosition, range,
                    entity => entity == comp.Owner || entity == _attachedEntity))
                {
                    if (comp.Gui != null)
                        comp.Gui.FirstDraw = true;

                    return;
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
                        continue;

                    // Predictions
                    if (doAfter.BreakOnUserMove)
                    {
                        if (userGrid != doAfter.UserGrid)
                        {
                            comp.Cancel(id, currentTime);
                            continue;
                        }
                    }

                    if (doAfter.BreakOnTargetMove)
                    {
                        if (EntityManager.TryGetEntity(doAfter.TargetUid, out var targetEntity) && targetEntity.Transform.Coordinates != doAfter.TargetGrid)
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

                // Remove any components that we no longer need to track
                foundComps.Add(comp);
            }

            foreach (var comp in foundComps)
            {
                if (!_knownComponents.Contains(comp))
                {
                    _knownComponents.Remove(comp);
                    comp.Disable();
                }
            }
        }
    }
}
