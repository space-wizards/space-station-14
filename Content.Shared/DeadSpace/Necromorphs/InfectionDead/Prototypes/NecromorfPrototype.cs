// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Humanoid;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead.Prototypes;

[Prototype("necromorf")]
public sealed partial class NecromorfPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public DamageSpecifier? Damage;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string DamageModifierSet { get; set; } = "Necromorf";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string Claws { get; set; } = string.Empty;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string Hardsuit { get; set; } = string.Empty;

    [DataField]
    public bool IsCanSpawnInfectionDead { get; set; } = true;

    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; } = new();

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeedMultiply = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Scale = 0f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ThresholdMultiply = 1.5f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string Sprite = string.Empty;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string State = string.Empty;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string MapKey = string.Empty;

    [DataField("useInventory")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsCanUseInventory = true;

    [DataField("slowOnDamage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsSlowOnDamage = true;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsAnimal = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HumanoidVisualLayers[] LayersToHide { get; set; } = new[]
    {
        HumanoidVisualLayers.LHand,
        HumanoidVisualLayers.RHand,
        HumanoidVisualLayers.LArm,
        HumanoidVisualLayers.RArm,
        HumanoidVisualLayers.Hair
    };
}
