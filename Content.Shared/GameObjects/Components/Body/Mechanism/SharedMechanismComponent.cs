#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Behavior;
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
                    if (old.Body == null)
                    {
                        RemovedFromPart(old);
                    }
                    else
                    {
                        RemovedFromPartInBody(old.Body, old);
                    }
                }

                if (value != null)
                {
                    if (value.Body == null)
                    {
                        AddedToPart(value);
                    }
                    else
                    {
                        AddedToPartInBody(value.Body, value);
                    }
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

        public void AddedToBody(IBody body)
        {
            DebugTools.AssertNotNull(Body);
            DebugTools.AssertNotNull(body);

            OnAddedToBody(body);

            foreach (var behavior in Owner.GetAllComponents<IMechanismBehavior>())
            {
                behavior.AddedToBody(body);
            }
        }

        public void AddedToPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(Part);
            DebugTools.AssertNotNull(part);

            Owner.Transform.AttachParent(part.Owner);
            OnAddedToPart(part);

            foreach (var behavior in Owner.GetAllComponents<IMechanismBehavior>().ToArray())
            {
                behavior.AddedToPart(part);
            }
        }

        public void AddedToPartInBody(IBody body, IBodyPart part)
        {
            DebugTools.AssertNotNull(Body);
            DebugTools.AssertNotNull(body);
            DebugTools.AssertNotNull(Part);
            DebugTools.AssertNotNull(part);

            Owner.Transform.AttachParent(part.Owner);
            OnAddedToPartInBody(body, part);

            foreach (var behavior in Owner.GetAllComponents<IMechanismBehavior>())
            {
                behavior.AddedToPartInBody(body, part);
            }
        }

        public void RemovedFromBody(IBody old)
        {
            DebugTools.AssertNull(Body);
            DebugTools.AssertNotNull(old);

            OnRemovedFromBody(old);

            foreach (var behavior in Owner.GetAllComponents<IMechanismBehavior>())
            {
                behavior.RemovedFromBody(old);
            }
        }

        public void RemovedFromPart(IBodyPart old)
        {
            DebugTools.AssertNull(Part);
            DebugTools.AssertNotNull(old);

            Owner.Transform.AttachToGridOrMap();
            OnRemovedFromPart(old);

            foreach (var behavior in Owner.GetAllComponents<IMechanismBehavior>())
            {
                behavior.RemovedFromPart(old);
            }
        }

        public void RemovedFromPartInBody(IBody oldBody, IBodyPart oldPart)
        {
            DebugTools.AssertNull(Body);
            DebugTools.AssertNotNull(oldBody);
            DebugTools.AssertNull(Part);
            DebugTools.AssertNotNull(oldPart);

            Owner.Transform.AttachToGridOrMap();
            OnRemovedFromPartInBody(oldBody, oldPart);

            foreach (var behavior in Owner.GetAllComponents<IMechanismBehavior>())
            {
                behavior.RemovedFromPartInBody(oldBody, oldPart);
            }
        }

        protected virtual void OnAddedToBody(IBody body) { }

        protected virtual void OnAddedToPart(IBodyPart part) { }

        protected virtual void OnAddedToPartInBody(IBody body, IBodyPart part) { }

        protected virtual void OnRemovedFromBody(IBody old) { }

        protected virtual void OnRemovedFromPart(IBodyPart old) { }

        protected virtual void OnRemovedFromPartInBody(IBody oldBody, IBodyPart oldPart) { }
    }
}
