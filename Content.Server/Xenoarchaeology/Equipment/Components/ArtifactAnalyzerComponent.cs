using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Construction.Prototypes;
using Content.Shared.MachineLinking;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class ArtifactAnalyzerComponent : Component
{
    /// <summary>
    /// How long it takes to analyze an artifact
    /// </summary>
    [DataField("analysisDuration", customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan AnalysisDuration = TimeSpan.FromSeconds(10); //TODO: change this back

    [ViewVariables(VVAccess.ReadWrite)]
    public float AnalysisDurationMulitplier = 1;

    [DataField("machinePartAnalysisDuration", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartAnalysisDuration = "ScanningModule";

    [DataField("partRatingAnalysisDurationMultiplier")]
    public float PartRatingAnalysisDurationMultiplier = 0.75f;

    [DataField("linkingPort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
    public readonly string LinkingPort = "ArtifactAnalyzerReceiver";


    [ViewVariables]
    public EntityUid? Console;

    #region Analysis Data
    [ViewVariables]
    public EntityUid? LastAnalyzedArtifact;

    [ViewVariables]
    public ArtifactNode? LastAnalyzedNode;

    [ViewVariables(VVAccess.ReadWrite)]
    public float? LastAnalyzedCompletion;
    #endregion
}

public struct ArtifactAnalyzerScanFinishedEvent
{

}
