using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder object material statistics (hardness / HP / broken threshold).
/// </summary>
[Prototype("cfMaterial")]
public sealed partial class CFMaterialPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>Hardness subtracted from Strike damage before HP loss.</summary>
    [DataField]
    public int Hardness = 5;

    [DataField]
    public int HitPoints = 20;

    /// <summary>Broken threshold (informational / future BT rules). Defaults to half HP.</summary>
    [DataField]
    public int BrokenThreshold;

    /// <summary>SS14 <c>damageModifierSet</c> ids that map to this material when inferring.</summary>
    [DataField]
    public List<string> DamageModifierSets = new();

    /// <summary>SS14 <c>damageContainer</c> ids that map to this material when inferring.</summary>
    [DataField]
    public List<string> DamageContainers = new();
}
