#nullable enable
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Body.Behavior
{
    public class BrainBehavior : MechanismBehavior
    {
        protected override void OnAddedToBody(IBody body)
        {
            base.OnAddedToBody(body);

            HandleMind(body.Owner, Owner);
        }

        protected override void OnAddedToPart(IBodyPart part)
        {
            base.OnAddedToPart(part);

            HandleMind(part.Owner, Owner, true);
        }

        protected override void OnAddedToPartInBody(IBody body, IBodyPart part)
        {
            base.OnAddedToPartInBody(body, part);

            HandleMind(body.Owner, Owner, true);
        }

        protected override void OnRemovedFromBody(IBody old)
        {
            base.OnRemovedFromBody(old);

            HandleMind(Part!.Owner, old.Owner);
        }

        protected override void OnRemovedFromPart(IBodyPart old)
        {
            base.OnRemovedFromPart(old);

            HandleMind(Owner, old.Owner);
        }

        protected override void OnRemovedFromPartInBody(IBody oldBody, IBodyPart oldPart)
        {
            base.OnRemovedFromPartInBody(oldBody, oldPart);

            HandleMind(oldBody.Owner, Owner);
        }

        private void HandleMind(IEntity newEntity, IEntity oldEntity, bool forceReturn = false)
        {
            var newMind = newEntity.EnsureComponent<MindComponent>();
            var oldMind = oldEntity.EnsureComponent<MindComponent>();
            var visitingEntity = oldMind.Mind?.VisitingEntity;

            if (!newEntity.HasComponent<IGhostOnMove>())
                newEntity.AddComponent<GhostOnMoveComponent>();

            // TODO: This is an awful solution.
            if (!newEntity.HasComponent<IMoverComponent>())
                newEntity.AddComponent<SharedDummyInputMoverComponent>();

            if (forceReturn)
            {
                if (visitingEntity?.HasComponent<GhostComponent>() == true)
                    visitingEntity.Delete();
                visitingEntity = null;
            }

            if (visitingEntity == null)
            {
                oldMind.Mind?.TransferTo(newEntity);
                return;
            }

            oldMind.Mind?.UnVisit(true);
            oldMind.Mind?.TransferTo(newEntity, true);
            newMind.Mind?.Visit(visitingEntity, true);
        }
    }
}
