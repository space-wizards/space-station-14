// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Explosion;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Abilities.ExplosionAbility.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ExplosionAbilityComponent : Component
{
    [DataField]
    public EntProtoId ExplosionAbilityAction = "ActionExplosionAbility";

    [DataField]
    public EntityUid? ExplosionAbilityActionEntity;

    [DataField]
    public ProtoId<ExplosionPrototype> TypeId = "MicroBomb";

    [DataField]
    public float TotalIntensity = 100f;

    [DataField]
    public float MaxTileIntensity = 10f;

    [DataField]
    public int NumberExplosions = 0;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int Explosions = 0;
}
