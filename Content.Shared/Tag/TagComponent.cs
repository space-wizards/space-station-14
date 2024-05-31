using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Tag
{
    [RegisterComponent, NetworkedComponent, Access(typeof(TagSystem))]
    public sealed partial class TagComponent : Component
    {
        [DataField("tags", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TagPrototype>))]
        [Access(typeof(TagSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public HashSet<string> Tags = new();
    }
}
