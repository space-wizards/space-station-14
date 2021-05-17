using System.Collections.Generic;
using Content.Client.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class WindowSystem : EntitySystem
    {
        private readonly Queue<IEntity> _dirtyEntities = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WindowSmoothDirtyEvent>(HandleDirtyEvent);
            SubscribeLocalEvent<WindowComponent, SnapGridPositionChangedEvent>(HandleSnapGridMove);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<WindowSmoothDirtyEvent>();
            UnsubscribeLocalEvent<WindowComponent, SnapGridPositionChangedEvent>(HandleSnapGridMove);
        }

        private void HandleDirtyEvent(WindowSmoothDirtyEvent ev)
        {
            if (ev.Sender.HasComponent<WindowComponent>())
            {
                _dirtyEntities.Enqueue(ev.Sender);
            }
        }

        private static void HandleSnapGridMove(EntityUid uid, WindowComponent component, SnapGridPositionChangedEvent args)
        {
            component.SnapGridOnPositionChanged();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            // Performance: This could be spread over multiple updates, or made parallel.
            while (_dirtyEntities.Count > 0)
            {
                var entity = _dirtyEntities.Dequeue();
                if (entity.Deleted)
                {
                    continue;
                }

                entity.GetComponent<WindowComponent>().UpdateSprite();
            }
        }
    }

    /// <summary>
    ///     Event raised by a <see cref="WindowComponent"/> when it needs to be recalculated.
    /// </summary>
    public sealed class WindowSmoothDirtyEvent : EntityEventArgs
    {
        public IEntity Sender { get; }

        public WindowSmoothDirtyEvent(IEntity sender)
        {
            Sender = sender;
        }
    }
}
