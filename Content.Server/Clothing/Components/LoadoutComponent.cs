using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
    public sealed class LoadoutComponent : Component
    {
        [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
        public string Prototype = string.Empty;
    }
}
