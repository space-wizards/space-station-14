// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Storage;

namespace Content.Shared.DeadSpace.Necromorphs.InfectorDead;

[RegisterComponent, NetworkedComponent]
public sealed partial class InfectorDeadComponent : Component
{
    [DataField("actionInfectionNecro", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionInfectionNecro = "ActionInfectionNecro";

    [DataField("actionInfectionNecroEntity")]
    public EntityUid? ActionInfectionNecroEntity;

    [DataField("spawned", required: true)]
    public List<EntitySpawnEntry> SpawnedEntities = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float InfectedDuration = 2.5f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float HealDuration = 12f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float Duration = 2.5f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool HasGland = true;

}
