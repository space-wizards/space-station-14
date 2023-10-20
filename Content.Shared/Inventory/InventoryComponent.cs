using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Inventory;

[RegisterComponent, NetworkedComponent]
[Access(typeof(InventorySystem))]
public sealed partial class InventoryComponent : Component
{
    [DataField("templateId", customTypeSerializer: typeof(PrototypeIdSerializer<InventoryTemplatePrototype>))]
    public string TemplateId { get; private set; } = "human";

    [DataField("speciesId")] public string? SpeciesId { get; set; }
}
