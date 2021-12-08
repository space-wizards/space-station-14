using System.Collections.Generic;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Components
{
    public abstract class SharedMechanismComponent : Component, ISerializationHooks
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "Mechanism";

        protected readonly Dictionary<int, object> OptionsCache = new();
        protected SharedBodyComponent? BodyCache;
        protected int IdHash;
        protected EntityUid? PerformerCache;
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
                        _entMan.EventBus.RaiseLocalEvent(Owner, new RemovedFromPartEvent(old));
                    }
                    else
                    {
                        _entMan.EventBus.RaiseLocalEvent(Owner, new RemovedFromPartInBodyEvent(old.Body, old));
                    }
                }

                if (value != null)
                {
                    if (value.Body == null)
                    {
                        _entMan.EventBus.RaiseLocalEvent(Owner, new AddedToPartEvent(value));
                    }
                    else
                    {
                        _entMan.EventBus.RaiseLocalEvent(Owner, new AddedToPartInBodyEvent(value.Body, value));
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
    }
}
