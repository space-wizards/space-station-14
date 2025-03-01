using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicSpireComponent : Component
{

    [DataField] public bool Enabled = false;
    [DataField] public float DrainRate = 550;
    [DataField] public float DrainThreshHold = 2500;

    [DataField]
    public HashSet<Gas> DrainGases =
    [
        Gas.Oxygen,
        Gas.Nitrogen,
        Gas.CarbonDioxide,
        Gas.WaterVapor,
        Gas.Ammonia,
        Gas.NitrousOxide,
    ];

    [DataField("gasMixture"), ViewVariables(VVAccess.ReadWrite)] public GasMixture Storage { get; private set; } = new();
    [DataField] public EntProtoId EntropyMote = "MaterialCosmicCultEntropy1";
    [DataField] public EntProtoId SpawnVFX = "CosmicGenericVFX";
}

[Serializable, NetSerializable]
public enum SpireVisuals : byte
{
    Status,
}

[Serializable, NetSerializable]
public enum SpireStatus : byte
{
    Off,
    On,
}
