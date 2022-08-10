using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components
{
    [RegisterComponent]
    public sealed class MechanismComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
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
                        _entMan.EventBus.RaiseLocalEvent(Owner, new RemovedFromPartEvent(old), true);
                    }
                    else
                    {
                        _entMan.EventBus.RaiseLocalEvent(Owner, new RemovedFromPartInBodyEvent(old.Body, old), true);
                    }
                }

                if (value != null)
                {
                    if (value.Body == null)
                    {
                        _entMan.EventBus.RaiseLocalEvent(Owner, new AddedToPartEvent(value), true);
                    }
                    else
                    {
                        _entMan.EventBus.RaiseLocalEvent(Owner, new AddedToPartInBodyEvent(value.Body, value), true);
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
        ///     <see cref="MechanismComponent"/> can fit into a <see cref="SharedBodyPartComponent"/>.
        /// </summary>
        [DataField("size")] public int Size { get; set; } = 1;

        /// <summary>
        ///     What kind of <see cref="SharedBodyPartComponent"/> this
        ///     <see cref="MechanismComponent"/> can be easily installed into.
        /// </summary>
        [DataField("compatibility")]
        public BodyPartCompatibility Compatibility { get; set; } = BodyPartCompatibility.Universal;
    }
}
