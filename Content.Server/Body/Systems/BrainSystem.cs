using Content.Server.Body.Components;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Movement.Components;

namespace Content.Server.Body.Systems
{
    public sealed class BrainSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrainComponent, MechanismAddedToBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
            SubscribeLocalEvent<BrainComponent, MechanismAddedToPartEvent>((uid, _, args) => HandleMind(args.Part, uid));
            SubscribeLocalEvent<BrainComponent, MechanismAddedToPartInBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
            SubscribeLocalEvent<BrainComponent, MechanismRemovedFromBodyEvent>(OnRemovedFromBody);
            SubscribeLocalEvent<BrainComponent, MechanismRemovedFromPartEvent>((uid, _, args) => HandleMind(uid, args.Part));
            SubscribeLocalEvent<BrainComponent, MechanismRemovedFromPartInBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
        }

        private void OnRemovedFromBody(EntityUid uid, BrainComponent component, MechanismRemovedFromBodyEvent args)
        {
            // This one needs to be special, okay?
            if (!EntityManager.TryGetComponent(uid, out MechanismComponent? mech))
                return;

            HandleMind((mech.Part!).Owner, args.Body);
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
