using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Inventory.New;

[RegisterComponent]
public class InventoryComponent : Component
{
    public sealed override string Name => "Inventory";

    [DataField("templateId", required: true,
        customTypeSerializer: typeof(PrototypeIdSerializer<InventoryTemplatePrototype>))]
    public string TemplateId { get; } = "human";
}
