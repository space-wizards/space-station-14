using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public sealed class TransformationParentComponent : Component
    {
        /// <summary>
        /// Whether or not the user can transform and revert at will
        /// </summary>
        [DataField("forced")]
        public bool Forced = false;

        /// <summary>
        /// Whether or not all items should be dropped on transformation
        /// </summary>
        [DataField("DropItem")]
        public bool DropItem = false;

        /// <summary>
        /// The entity prototype the user will transform into
        /// </summary>
        [DataField("transformPrototypeId")]
        public string? TransformPrototype = "MobCarp";

        [DataField("actionId", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
        public string ActionId = "Transform";

        [DataField("action")]
        public InstantAction? TransformAction = null;
    }
}
