using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Construction.Prototypes;

namespace Content.Shared.Construction;

/// <summary>
/// Used for starting a certain item crafting recipe when activated.
/// </summary>
[RegisterComponent]
public sealed partial class StartItemConstructionOnActivateComponent : Component
{
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<ConstructionPrototype>))]
    public string? Prototype { get; private set; }
}
