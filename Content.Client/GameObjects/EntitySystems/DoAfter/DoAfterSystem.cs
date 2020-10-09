#nullable enable
using System.Linq;
using Content.Client.GameObjects.Components;
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
        [Dependency] private readonly IEntityManager _entityManager = default!;

        /// <summary>
        ///     Rather than checking attached player every tick we'll just store it from the message.
        /// </summary>
        private IEntity? _player;

        /// <summary>
        ///     We'll use an excess time so stuff like finishing effects can show.
        /// </summary>
        public const float ExcessTime = 0.5f;

        public DoAfterGui? Gui { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlayerAttachSysMessage>(message => HandlePlayerAttached(message.AttachedEntity));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            Gui?.Dispose();
            Gui = null;
            _player = null;
        }

        private void HandlePlayerAttached(IEntity? entity)
        {
            _player = entity;
            // Setup the GUI and pass the new data to it if applicable.
            Gui?.Detached();

            if (entity == null)
            {
                return;
            }

            Gui ??= new DoAfterGui();
            Gui.AttachedEntity = entity;

            if (entity.TryGetComponent(out DoAfterComponent? doAfterComponent))
            {
                foreach (var (_, doAfter) in doAfterComponent.DoAfters)
                {
                    Gui.AddDoAfter(doAfter);
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var currentTime = _gameTiming.CurTime;

            if (_player?.IsValid() != true)
            {
                return;
            }

            if (!_player.TryGetComponent(out DoAfterComponent? doAfterComponent))
            {
                return;
            }

            var doAfters = doAfterComponent.DoAfters.ToList();
            if (doAfters.Count == 0)
            {
                return;
            }

            var userGrid = _player.Transform.Coordinates;

            // Check cancellations / finishes
            foreach (var (id, doAfter) in doAfters)
            {
                var elapsedTime = (currentTime - doAfter.StartTime).TotalSeconds;

                // If we've passed the final time (after the excess to show completion graphic) then remove.
                if (elapsedTime > doAfter.Delay + ExcessTime)
                {
                    Gui?.RemoveDoAfter(id);
                    doAfterComponent.Remove(doAfter);
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
                    if (userGrid != doAfter.UserGrid)
                    {
                        doAfterComponent.Cancel(id, currentTime);
                        continue;
                    }
                }

                if (doAfter.BreakOnTargetMove)
                {
                    if (!_entityManager.TryGetEntity(doAfter.TargetUid, out var targetEntity))
                    {
                        // Cancel if the target entity doesn't exist.
                        doAfterComponent.Cancel(id, currentTime);
                        continue;
                    }

                    if (targetEntity.Transform.Coordinates != doAfter.TargetGrid)
                    {
                        doAfterComponent.Cancel(id, currentTime);
                        continue;
                    }
                }
            }

            var count = doAfterComponent.CancelledDoAfters.Count;
            // Remove cancelled DoAfters after ExcessTime has elapsed
            for (var i = count - 1; i >= 0; i--)
            {
                var cancelled = doAfterComponent.CancelledDoAfters[i];
                if ((currentTime - cancelled.CancelTime).TotalSeconds > ExcessTime)
                {
                    doAfterComponent.Remove(cancelled.Message);
                }
            }
        }
    }
}
