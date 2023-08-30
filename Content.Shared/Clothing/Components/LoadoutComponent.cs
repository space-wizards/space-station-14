using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LoadoutComponent : Component
{
    /// <summary>
    /// A list of starting gears, of which one will be given.
    /// All elements are weighted the same in the list.
    /// </summary>
    [DataField("prototypes", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<StartingGearPrototype>)), AutoNetworkedField]
    public List<string>? Prototypes;
}
