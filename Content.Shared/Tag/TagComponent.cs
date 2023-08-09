using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Tag
{
    [RegisterComponent, Access(typeof(TagSystem))]
    public sealed class TagComponent : Component
    {
        [DataField("tags", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TagPrototype>))]
        [Access(typeof(TagSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public readonly HashSet<string> Tags = new();
    }
}
