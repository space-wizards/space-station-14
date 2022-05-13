using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Polymorph.Components
{
    [RegisterComponent]
    public sealed class PolymorphedEntityComponent : Component
    {
        /// <summary>
        /// The original entity that the player will revert back into
        /// </summary>
        [DataField("parent")]
        public EntityUid? Parent = null;

        /// <summary>
        /// The container that holds the parent entity while transformed
        /// </summary>
        [DataField("parentContainer")]
        public Container ParentContainer = default!;

        [DataField("forced")]
        public bool Forced = default!;
    }
}
