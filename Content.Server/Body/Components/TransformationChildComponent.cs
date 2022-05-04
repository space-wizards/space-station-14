using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public sealed class TransformationChildComponent : Component
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

        [DataField("actionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
        public string ActionId = "Revert";

        [DataField("action")]
        public InstantAction? Action = null;
    }
}
