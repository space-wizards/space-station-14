#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public abstract class SharedMechanismComponent : Component, IMechanism
    {
        public override string Name => "Mechanism";

        private IBodyPart? _part;

        protected readonly Dictionary<int, object> OptionsCache = new Dictionary<int, object>();

        protected IBody? BodyCache;

        protected int IdHash;

        protected IEntity? PerformerCache;

        public IBody? Body => Part?.Body;

        public IBodyPart? Part
        {
            get => _part;
            set
            {
                if (_part == value)
                {
                    return;
                }

                var old = _part;
                _part = value;

                if (old != null)
                {
                    OnRemovedFromPart(old);
                }

                if (value != null)
                {
                    OnAddedToPart();
                }
            }
        }

        public string Description { get; set; } = string.Empty;

        public string ExamineMessage { get; set; } = string.Empty;

        public int MaxDurability { get; set; }

        public int CurrentDurability { get; set; }

        public int DestroyThreshold { get; set; }

        // TODO BODY
        public int Resistance { get; set; }

        // TODO BODY OnSizeChanged
        public int Size { get; set; }

        public BodyPartCompatibility Compatibility { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, m => m.Description, "description", string.Empty);

            serializer.DataField(this, m => m.ExamineMessage, "examineMessage", string.Empty);

            serializer.DataField(this, m => m.MaxDurability, "maxDurability", 10);

            serializer.DataField(this, m => m.CurrentDurability, "currentDurability", MaxDurability);

            serializer.DataField(this, m => m.DestroyThreshold, "destroyThreshold", -MaxDurability);

            serializer.DataField(this, m => m.Resistance, "resistance", 0);

            serializer.DataField(this, m => m.Size, "size", 1);

            serializer.DataField(this, m => m.Compatibility, "compatibility", BodyPartCompatibility.Universal);
        }

        public void AddedToBody()
        {
            DebugTools.AssertNotNull(Body);

            OnAddedToBody();

            foreach (var behavior in Owner.GetMechanismBehaviors())
            {
                behavior.AddedToBody();
            }
        }

        public void RemovedFromBody(IBody old)
        {
            OnRemovedFromBody(old);

            foreach (var behavior in Owner.GetMechanismBehaviors())
            {
                behavior.RemovedFromBody(old);
            }
        }

        public void AddedToPart()
        {
            DebugTools.AssertNotNull(Part);

            Owner.Transform.AttachParent(Part!.Owner);
            OnAddedToPart();

            foreach (var behavior in Owner.GetMechanismBehaviors())
            {
                behavior.AddedToPart();
            }
        }

        public void RemovedFromPart(IBodyPart old)
        {
            Owner.Transform.AttachToGridOrMap();
            OnRemovedFromPart(old);

            foreach (var behavior in Owner.GetMechanismBehaviors())
            {
                behavior.RemovedFromPart(old);
            }
        }

        public void AddedToPartInBody()
        {
            DebugTools.AssertNotNull(Body);
            DebugTools.AssertNotNull(Part);

            Owner.Transform.AttachParent(Part!.Owner);
            OnAddedToPartInBody();

            foreach (var behavior in Owner.GetMechanismBehaviors())
            {
                behavior.AddedToPartInBody();
            }
        }

        public void RemovedFromPartInBody(IBody? oldBody, IBodyPart? oldPart)
        {
            Owner.Transform.AttachToGridOrMap();
            OnRemovedFromPartInBody();

            foreach (var behavior in Owner.GetMechanismBehaviors())
            {
                behavior.RemovedFromPartInBody(oldBody, oldPart);
            }
        }

        protected virtual void OnAddedToBody() { }

        protected virtual void OnRemovedFromBody(IBody old) { }

        protected virtual void OnAddedToPart() { }

        protected virtual void OnRemovedFromPart(IBodyPart old) { }

        protected virtual void OnAddedToPartInBody() { }

        protected virtual void OnRemovedFromPartInBody() { }
    }
}
