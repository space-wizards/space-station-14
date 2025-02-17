// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.Abilities.ExplosionAbility.Components;

[RegisterComponent, NetworkedComponent]

public sealed partial class ExplosionAbilityComponent : Component
{
    [DataField("ExplosionAbilityAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ExplosionAbilityAction = "ActionExplosionAbility";

    [DataField("ExplosionAbilityActionEntity")]
    public EntityUid? ExplosionAbilityActionEntity;

    [DataField("typeId")]
    public string TypeId = "MicroBomb";

    [DataField("totalIntensity")]
    public float TotalIntensity = 100f;

    [DataField("maxTileIntensity")]
    public float MaxTileIntensity = 10f;

    [DataField]
    public int NumberExplosions = 0;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int Explosions = 0;
}
