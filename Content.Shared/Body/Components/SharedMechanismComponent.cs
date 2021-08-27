using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Components
{
    public abstract class SharedMechanismComponent : Component, ISerializationHooks
    {
        public override string Name => "Mechanism";

        protected readonly Dictionary<int, object> OptionsCache = new();
        protected SharedBodyComponent? BodyCache;
        protected int IdHash;
        protected IEntity? PerformerCache;
        private SharedBodyPartComponent? _part;

        public SharedBodyComponent? Body => Part?.Body;

        public SharedBodyPartComponent? Part
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

        [DataField("maxDurability")] public int MaxDurability { get; set; } = 10;

        [DataField("currentDurability")] public int CurrentDurability { get; set; } = 10;

        [DataField("destroyThreshold")] public int DestroyThreshold { get; set; } = -10;

        // TODO BODY: Surgery description and adding a message to the examine tooltip of the entity that owns this mechanism
        // TODO BODY
        [DataField("resistance")] public int Resistance { get; set; } = 0;

        // TODO BODY OnSizeChanged
        /// <summary>
        ///     Determines whether this
        ///     <see cref="SharedMechanismComponent"/> can fit into a <see cref="SharedBodyPartComponent"/>.
        /// </summary>
        [DataField("size")] public int Size { get; set; } = 1;

        /// <summary>
        ///     What kind of <see cref="SharedBodyPartComponent"/> this
        ///     <see cref="SharedMechanismComponent"/> can be easily installed into.
        /// </summary>
        [DataField("compatibility")]
        public BodyPartCompatibility Compatibility { get; set; } = BodyPartCompatibility.Universal;

        // TODO BODY Turn these into event listeners so they dont need to be exposed
        public void AddedToBody(SharedBodyComponent body)
        {
            DebugTools.AssertNotNull(Body);
            DebugTools.AssertNotNull(body);
        }

        public void AddedToPart(SharedBodyPartComponent part)
        {
            DebugTools.AssertNotNull(Part);
            DebugTools.AssertNotNull(part);

            Owner.Transform.AttachParent(part.Owner);
        }

        public void AddedToPartInBody(SharedBodyComponent body, SharedBodyPartComponent part)
        {
            DebugTools.AssertNotNull(Body);
            DebugTools.AssertNotNull(body);
            DebugTools.AssertNotNull(Part);
            DebugTools.AssertNotNull(part);

            Owner.Transform.AttachParent(part.Owner);
        }

        public void RemovedFromBody(SharedBodyComponent old)
        {
            DebugTools.AssertNull(Body);
            DebugTools.AssertNotNull(old);
        }

        public void RemovedFromPart(SharedBodyPartComponent old)
        {
            DebugTools.AssertNull(Part);
            DebugTools.AssertNotNull(old);

            Owner.Transform.AttachToGridOrMap();
        }

        public void RemovedFromPartInBody(SharedBodyComponent oldBody, SharedBodyPartComponent oldPart)
        {
            DebugTools.AssertNull(Body);
            DebugTools.AssertNotNull(oldBody);
            DebugTools.AssertNull(Part);
            DebugTools.AssertNotNull(oldPart);

            Owner.Transform.AttachToGridOrMap();
        }
    }
}
