using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
    public sealed class LoadoutComponent : Component
    {
        /// <summary>
        /// A list of starting gears, of which one will be given.
        /// All elements are weighted the same in the list.
        /// </summary>
        [DataField("prototypes", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<StartingGearPrototype>))]
        public List<string>? Prototypes;
    }
}
