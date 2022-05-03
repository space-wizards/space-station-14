using Content.Server.Botany.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.UseWith;

[RegisterComponent]
[Friend(typeof(UseWithSystem))]
public sealed class UseWithComponent : Component
{
    [DataField("spawnedPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnedPrototype = "MaterialWoodPlank1";

    [DataField("spawnCount")] 
    public int SpawnCount = 2;
    
    [ViewVariables]
    [DataField("whitelist")] 
    public EntityWhitelist? UseWithWhitelist;
}
