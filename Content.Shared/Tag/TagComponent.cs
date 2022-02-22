using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Tag
{
    [RegisterComponent, Friend(typeof(TagSystem))]
    public sealed class TagComponent : Component, ISerializationHooks
    {
        [ViewVariables]
        [DataField("tags", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TagPrototype>))]
        public readonly HashSet<string> Tags = new();
    }
}
