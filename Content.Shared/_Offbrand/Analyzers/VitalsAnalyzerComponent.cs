using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Analyzers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(VitalsAnalyzerSystem))]
public sealed partial class VitalsAnalyzerComponent : Component
{
    [DataField, AutoNetworkedField]
    public VitalsData? Data;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class VitalsData
{
    [DataField]
    public float BrainHealth;

    [DataField]
    public (int, int) BloodPressure;

    [DataField]
    public int HeartRate;

    [DataField]
    public int Etco2;

    [DataField]
    public int RespiratoryRate;

    [DataField]
    public float Spo2;

    [DataField]
    public LocId Etco2Name;

    [DataField]
    public LocId Etco2GasName;

    [DataField]
    public LocId Spo2Name;

    [DataField]
    public LocId Spo2GasName;

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, (FixedPoint2 InBloodstream, FixedPoint2 Metabolites)>? Reagents;

    [DataField]
    public bool NonMedicalReagents;

    [DataField]
    public float BloodLevel;
}
