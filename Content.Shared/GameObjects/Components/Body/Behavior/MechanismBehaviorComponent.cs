#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

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
                AddedToPart(Part);
            }
            else
            {
                AddedToPartInBody(Body, Part);
            }
        }

        public abstract void Update(float frameTime);

        public void AddedToBody(IBody body)
        {
            DebugTools.AssertNotNull(Body);
            DebugTools.AssertNotNull(body);

            OnAddedToBody(body);
        }

        public void AddedToPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(Part);
            DebugTools.AssertNotNull(part);

            OnAddedToPart(part);
        }

        public void AddedToPartInBody(IBody body, IBodyPart part)
        {
            DebugTools.AssertNotNull(Body);
            DebugTools.AssertNotNull(body);
            DebugTools.AssertNotNull(Part);
            DebugTools.AssertNotNull(part);

            OnAddedToPartInBody(body, part);
        }

        public void RemovedFromBody(IBody old)
        {
            DebugTools.AssertNull(Body);
            DebugTools.AssertNotNull(old);

            OnRemovedFromBody(old);
        }

        public void RemovedFromPart(IBodyPart old)
        {
            DebugTools.AssertNull(Part);
            DebugTools.AssertNotNull(old);

            OnRemovedFromPart(old);
        }

        public void RemovedFromPartInBody(IBody oldBody, IBodyPart oldPart)
        {
            DebugTools.AssertNull(Body);
            DebugTools.AssertNull(Part);
            DebugTools.AssertNotNull(oldBody);
            DebugTools.AssertNotNull(oldPart);

            OnRemovedFromPartInBody(oldBody, oldPart);
        }

        protected virtual void OnAddedToBody(IBody body) { }

        protected virtual void OnAddedToPart(IBodyPart part) { }

        protected virtual void OnAddedToPartInBody(IBody body, IBodyPart part) { }

        protected virtual void OnRemovedFromBody(IBody old) { }

        protected virtual void OnRemovedFromPart(IBodyPart old) { }

        protected virtual void OnRemovedFromPartInBody(IBody oldBody, IBodyPart oldPart) { }
    }
}
