using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicSpireComponent : Component
{

    [ViewVariables(VVAccess.ReadWrite)] public bool Enabled = false;
    [DataField, ViewVariables(VVAccess.ReadWrite)] public float DrainRate = 550;
    [DataField, ViewVariables(VVAccess.ReadWrite)] public float DrainThreshHold = 2500;

    [DataField("drainGases")]
    public HashSet<Gas> DrainGases = new()
    {
        Gas.Oxygen,
        Gas.Nitrogen,
        Gas.CarbonDioxide,
        Gas.WaterVapor,
        Gas.Ammonia,
        Gas.NitrousOxide,
    };

    [DataField("gasMixture"), ViewVariables(VVAccess.ReadWrite)] public GasMixture Storage { get; private set; } = new();
    [DataField] public EntProtoId EntropyMote = "MaterialCosmicCultEntropy1";
    [DataField] public EntProtoId SpawnVFX = "CosmicGenericVFX";
}

[Serializable, NetSerializable]
public enum SpireVisuals : byte
{
    Status
}
[Serializable, NetSerializable]
public enum SpireStatus : byte
{
    Off,
    On
}
