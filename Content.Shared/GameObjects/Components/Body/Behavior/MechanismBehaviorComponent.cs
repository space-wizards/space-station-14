#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    public abstract class MechanismBehaviorComponent : Component, IMechanismBehavior
    {
        public IBody? Body => Part?.Body;

        public IBodyPart? Part => Mechanism?.Part;

        public IMechanism? Mechanism => Owner.GetComponentOrNull<IMechanism>();

        protected override void Startup()
        {
            base.Startup();

            if (Part == null)
            {
                return;
            }

            if (Body == null)
            {
                AddedToPart();
            }
            else
            {
                AddedToPartInBody();
            }
        }

        public abstract void Update(float frameTime);

        public void AddedToBody()
        {
            OnAddedToBody();
        }

        public void AddedToPart()
        {
            OnAddedToPart();
        }

        public void RemovedFromBody(IBody old)
        {
            OnRemovedFromBody(old);
        }

        public void RemovedFromPart(IBodyPart old)
        {
            OnRemovedFromPart(old);
        }

        public void AddedToPartInBody()
        {
            OnAddedToPartInBody();
        }

        public void RemovedFromPartInBody(IBody? oldBody, IBodyPart? oldPart)
        {
            OnRemovedFromPartInBody(oldBody, oldPart);
        }

        protected virtual void OnAddedToBody() { }

        protected virtual void OnAddedToPart() { }

        protected virtual void OnRemovedFromBody(IBody old) { }

        protected virtual void OnRemovedFromPart(IBodyPart old) { }

        protected virtual void OnAddedToPartInBody() { }

        protected virtual void OnRemovedFromPartInBody(IBody? oldBody, IBodyPart? oldPart) { }
    }
}
