using Content.Shared.Polymorph;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Polymorph.Components
{
    [RegisterComponent]
    public sealed class PolymorphedEntityComponent : Component
    {
        /// <summary>
        /// The polymorph prototype, used to track various information
        /// about the polymorph
        /// </summary>
        public PolymorphPrototype Prototype = default!;

        /// <summary>
        /// The original entity that the player will revert back into
        /// </summary>
        [DataField("parent", required: true)]
        public EntityUid Parent = new();

        /// <summary>
        /// The amount of time that has passed since the entity was created
        /// used for tracking the duration
        /// </summary>
        [DataField("time")]
        public float Time = 0;
    }
}
