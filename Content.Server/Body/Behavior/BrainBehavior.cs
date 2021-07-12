#nullable enable
using Content.Server.Ghost;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Behavior
{
    public class BrainBehavior : MechanismBehavior
    {
        protected override void OnAddedToBody(SharedBodyComponent body)
        {
            base.OnAddedToBody(body);

            HandleMind(body.Owner, Owner);
        }

        protected override void OnAddedToPart(SharedBodyPartComponent part)
        {
            base.OnAddedToPart(part);

            HandleMind(part.Owner, Owner);
        }

        protected override void OnAddedToPartInBody(SharedBodyComponent body, SharedBodyPartComponent part)
        {
            base.OnAddedToPartInBody(body, part);

            HandleMind(body.Owner, Owner);
        }

        protected override void OnRemovedFromBody(SharedBodyComponent old)
        {
            base.OnRemovedFromBody(old);

            HandleMind(Part!.Owner, old.Owner);
        }

        protected override void OnRemovedFromPart(SharedBodyPartComponent old)
        {
            base.OnRemovedFromPart(old);

            HandleMind(Owner, old.Owner);
        }

        protected override void OnRemovedFromPartInBody(SharedBodyComponent oldBody, SharedBodyPartComponent oldPart)
        {
            base.OnRemovedFromPartInBody(oldBody, oldPart);

            HandleMind(oldBody.Owner, Owner);
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
