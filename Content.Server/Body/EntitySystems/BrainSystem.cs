using Content.Server.Body.Components;
using Content.Server.Ghost;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.EntitySystems
{
    public class BrainSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            // TODO MIRROR this doesn't feel right but I'll suss it out later..
            SubscribeLocalEvent<BrainComponent, AddedToBodyEvent>(OnAddedToBody);
            SubscribeLocalEvent<BrainComponent, AddedToPartEvent>(OnAddedToPart);
            SubscribeLocalEvent<BrainComponent, AddedToPartInBodyEvent>(OnAddedToPartInBody);
            SubscribeLocalEvent<BrainComponent, RemovedFromBodyEvent>(OnRemovedFromBody);
            SubscribeLocalEvent<BrainComponent, RemovedFromPartEvent>(OnRemovedFromPart);
            SubscribeLocalEvent<BrainComponent, RemovedFromPartInBodyEvent>(OnRemovedFromPartInBody);
        }

        private void OnAddedToBody(EntityUid uid, BrainComponent component, AddedToBodyEvent args)
        {
            HandleMind(args.Body.Owner, EntityManager.GetEntity(uid));
        }

        private void OnAddedToPart(EntityUid uid, BrainComponent component, AddedToPartEvent args)
        {
            HandleMind(args.Part.Owner, EntityManager.GetEntity(uid));
        }

        private void OnAddedToPartInBody(EntityUid uid, BrainComponent component, AddedToPartInBodyEvent args)
        {
            HandleMind(args.Body.Owner, EntityManager.GetEntity(uid));
        }

        private void OnRemovedFromBody(EntityUid uid, BrainComponent component, RemovedFromBodyEvent args)
        {
            HandleMind(EntityManager.GetEntity(uid), args.Body.Owner);
        }

        private void OnRemovedFromPart(EntityUid uid, BrainComponent component, RemovedFromPartEvent args)
        {
            HandleMind(EntityManager.GetEntity(uid), args.Part.Owner);
        }

        private void OnRemovedFromPartInBody(EntityUid uid, BrainComponent component, RemovedFromPartInBodyEvent args)
        {
            HandleMind(args.Body.Owner, args.Part.Owner);
        }

        private void HandleMind(IEntity newEntity, IEntity oldEntity)
        {
            newEntity.EnsureComponent<MindComponent>();
            var oldMind = oldEntity.EnsureComponent<MindComponent>();

            if (!newEntity.HasComponent<IGhostOnMove>())
                newEntity.AddComponent<GhostOnMoveComponent>();

            // TODO: This is an awful solution.
            if (!newEntity.HasComponent<IMoverComponent>())
                newEntity.AddComponent<SharedDummyInputMoverComponent>();

            oldMind.Mind?.TransferTo(newEntity);
        }
    }
}
