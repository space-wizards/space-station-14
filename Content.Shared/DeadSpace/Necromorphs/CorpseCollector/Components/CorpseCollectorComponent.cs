// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Random;

namespace Content.Shared.DeadSpace.Necromorphs.CorpseCollector.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CorpseCollectorComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTickForRegen = TimeSpan.FromSeconds(0);

    [DataField]
    public EntProtoId ActionAbsorptionDeadNecro = "ActionAbsorptionDeadNecro";

    [DataField]
    public EntityUid? ActionAbsorptionDeadNecroEntity;

    [DataField]
    public EntProtoId ActionSpawnPointNecro = "ActionSpawnPointNecro";

    [DataField]
    public EntityUid? ActionSpawnPointEntity;

    [DataField]
    public EntProtoId ActionSpawnLeviathan = "ActionSpawnLeviathan";

    [DataField]
    public EntityUid? ActionSpawnLeviathanEntity;

    [DataField("proto")]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId LeviathanId = "MobLeviathanNecro";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float BuffDamage = 1.11f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float BuffSpeed = 1.05f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float BuffHeal = 1.1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float PassiveHealingMultiplier = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeedMultiplier = 1.0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxAbsorptions = 10;

    [ViewVariables(VVAccess.ReadWrite)]
    public int CountAbsorptions = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public int CountNecroDoDebuff = 0;

    [DataField]
    public int CountNecroDoDebuffMax = 1;

    [DataField]
    public ProtoId<WeightedRandomEntityPrototype> MobIds = "CorpseCollectorMobToSpawn";

    [DataField]
    public float AbsorptionDuration = 3f;

    [DataField]
    public DamageSpecifier PassiveHealing = new()
    {
        DamageDict = new()
        {
            { "Blunt", -1 },
            { "Slash", -1 },
            { "Piercing", -1 },
            { "Heat", -1 },
            { "Shock", -1 }
        }
    };

    #region Visualizer

    [DataField("state")]
    public string State = "lvl1";

    [DataField("lvl2")]
    public string Lvl2State = "lvl2";

    [DataField("lvl3")]
    public string Lvl3State = "lvl3";

    #endregion
}
