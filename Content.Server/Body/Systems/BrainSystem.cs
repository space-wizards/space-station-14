using Content.Server.Body.Components;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Movement.Components;

namespace Content.Server.Body.Systems
{
    public sealed class BrainSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrainComponent, AddedToBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
            SubscribeLocalEvent<BrainComponent, AddedToPartEvent>((uid, _, args) => HandleMind(args.Part, uid));
            SubscribeLocalEvent<BrainComponent, AddedToPartInBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
            SubscribeLocalEvent<BrainComponent, RemovedFromBodyEvent>(OnRemovedFromBody);
            SubscribeLocalEvent<BrainComponent, RemovedFromPartEvent>((uid, _, args) => HandleMind(uid, args.Old));
            SubscribeLocalEvent<BrainComponent, RemovedFromPartInBodyEvent>((uid, _, args) => HandleMind(args.OldBody, uid));
        }

        private void OnRemovedFromBody(EntityUid uid, BrainComponent component, RemovedFromBodyEvent args)
        {
            // This one needs to be special, okay?
            if (!EntityManager.TryGetComponent(uid, out OrganComponent? organ) ||
                organ.ParentSlot is not {Parent: var parent})
                return;

            HandleMind(parent, args.Old);
        }

        private void HandleMind(EntityUid newEntity, EntityUid oldEntity)
        {
            EntityManager.EnsureComponent<MindComponent>(newEntity);
            var oldMind = EntityManager.EnsureComponent<MindComponent>(oldEntity);

            EnsureComp<GhostOnMoveComponent>(newEntity);
            if (HasComp<BodyComponent>(newEntity))
                Comp<GhostOnMoveComponent>(newEntity).MustBeDead = true;

            // TODO: This is an awful solution.
            EnsureComp<InputMoverComponent>(newEntity);

            oldMind.Mind?.TransferTo(newEntity);
        }
    }
}
