using Content.Shared.Polymorph;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Polymorph.Components
{
    [RegisterComponent]
    public sealed partial class PolymorphableComponent : Component
    {
        /// <summary>
        /// A list of all the polymorphs that the entity has.
        /// Used to manage them and remove them if needed.
        /// </summary>
        public Dictionary<string, EntityUid>? PolymorphActions = null;

        /// <summary>
        /// The polymorphs that the entity starts out being able to do.
        /// </summary>
        [DataField("innatePolymorphs", customTypeSerializer : typeof(PrototypeIdListSerializer<PolymorphPrototype>))]
        public List<string>? InnatePolymorphs;
    }
}
