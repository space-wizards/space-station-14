using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class GhostSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, MindRemovedMessage>(OnMindRemovedMessage);
            SubscribeLocalEvent<GhostComponent, MindUnvisitedMessage>(OnMindUnvisitedMessage);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<GhostComponent, MindRemovedMessage>(OnMindRemovedMessage);
            UnsubscribeLocalEvent<GhostComponent, MindUnvisitedMessage>(OnMindUnvisitedMessage);
        }

        private void OnMindRemovedMessage(EntityUid uid, GhostComponent component, MindRemovedMessage args)
        {
            DeleteEntity(uid);
        }

        private void OnMindUnvisitedMessage(EntityUid uid, GhostComponent component, MindUnvisitedMessage args)
        {
            DeleteEntity(uid);
        }

        private void DeleteEntity(EntityUid uid)
        {
            if (!EntityManager.TryGetEntity(uid, out var entity)
                || entity.Deleted == true
                || entity.LifeStage == EntityLifeStage.Terminating)
                return;

            if (entity.TryGetComponent<MindComponent>(out var mind))
                mind.GhostOnShutdown = false;
            entity.Delete();
        }
    }
}
