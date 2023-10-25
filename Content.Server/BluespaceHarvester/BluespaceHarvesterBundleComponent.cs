using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.BluespaceHarvester;

[RegisterComponent]
public sealed partial class BluespaceHarvesterBundleComponent : Component
{
    [DataField("contents")]
    public List<EntityBundleEntity> Contents = new();
}

[Serializable, DataDefinition]
public partial struct EntityBundleEntity
{
    [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string? PrototypeId = null;

    [DataField("amount"), ViewVariables(VVAccess.ReadWrite)]
    public int Amount = 1;
}
