using Content.Server.Body.Components;
using Content.Server.Ghost;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Shared.Body.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Systems
{
    public class BrainSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrainComponent, AddedToBodyEvent>((uid, component, args) => HandleMind(args.Body.OwnerUid, uid));
            SubscribeLocalEvent<BrainComponent, AddedToPartEvent>((uid, component, args) => HandleMind(args.Part.OwnerUid, uid));
            SubscribeLocalEvent<BrainComponent, AddedToPartInBodyEvent>((uid, component, args) => HandleMind(args.Body.OwnerUid, uid));
            SubscribeLocalEvent<BrainComponent, RemovedFromBodyEvent>(OnRemovedFromBody);
            SubscribeLocalEvent<BrainComponent, RemovedFromPartEvent>((uid, component, args) => HandleMind(uid, args.Old.OwnerUid));
            SubscribeLocalEvent<BrainComponent, RemovedFromPartInBodyEvent>((uid, component, args) => HandleMind(args.OldBody.OwnerUid, uid));
        }

        private void OnRemovedFromBody(EntityUid uid, BrainComponent component, RemovedFromBodyEvent args)
        {
            // This one needs to be special, okay?
            if (!EntityManager.TryGetComponent(uid, out MechanismComponent mech))
                return;

            HandleMind(mech.Part!.OwnerUid, args.Old.OwnerUid);
        }

        private void HandleMind(EntityUid newEntity, EntityUid oldEntity)
        {
            EntityManager.EnsureComponent<MindComponent>(newEntity);
            var oldMind = EntityManager.EnsureComponent<MindComponent>(oldEntity);

            if (!EntityManager.HasComponent<IGhostOnMove>(newEntity))
                EntityManager.AddComponent<GhostOnMoveComponent>(newEntity);

            // TODO: This is an awful solution.
            if (!EntityManager.HasComponent<IMoverComponent>(newEntity))
                EntityManager.AddComponent<SharedDummyInputMoverComponent>(newEntity);

            oldMind.Mind?.TransferTo(EntityManager.GetEntity(newEntity));
        }
    }
}
