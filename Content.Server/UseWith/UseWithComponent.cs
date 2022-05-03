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
    public string SpawnedPrototype;

    [DataField("spawnCount")] 
    public int SpawnCount;
    
    [ViewVariables]
    [DataField("whitelist")] 
    public EntityWhitelist? UseWithWhitelist;
}
