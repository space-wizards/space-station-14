using Content.Server.Botany.Systems;
using Content.Server.Construction;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Access(typeof(SeedExtractorSystem))]
public sealed partial class SeedExtractorComponent : Component
{
    /// <summary>
    /// The minimum amount of seed packets dropped with no machine upgrades.
    /// </summary>
    [DataField("baseMinSeeds"), ViewVariables(VVAccess.ReadWrite)]
    public int BaseMinSeeds = 1;

    /// <summary>
    /// The maximum amount of seed packets dropped with no machine upgrades.
    /// </summary>
    [DataField("baseMaxSeeds"), ViewVariables(VVAccess.ReadWrite)]
    public int BaseMaxSeeds = 3;

    /// <summary>
    /// Modifier to the amount of seeds outputted, set on <see cref="RefreshPartsEvent"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float SeedAmountMultiplier;

    /// <summary>
    /// Machine part whose rating modifies the amount of seed packets dropped.
    /// </summary>
    [DataField("machinePartYieldAmount", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartSeedAmount = "Manipulator";

    /// <summary>
    /// How much the machine part quality affects the amount of seeds outputted.
    /// Going up a tier will multiply the seed output by this amount.
    /// </summary>
    [DataField("partRatingSeedAmountMultiplier")]
    public float PartRatingSeedAmountMultiplier = 1.5f;
}
