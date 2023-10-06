using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// The console that is used for artifact analysis
/// </summary>
[RegisterComponent]
public sealed partial class AnalysisConsoleComponent : Component
{
    /// <summary>
    /// The analyzer entity the console is linked.
    /// Can be null if not linked.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AnalyzerEntity;

    /// <summary>
    /// The machine linking port for the analyzer
    /// </summary>
    [DataField("linkingPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string LinkingPort = "ArtifactAnalyzerSender";

    /// <summary>
    /// The sound played when an artifact has points extracted.
    /// </summary>
    [DataField("extractSound")]
    public SoundSpecifier ExtractSound = new SoundPathSpecifier("/Audio/Effects/radpulse11.ogg");

    /// <summary>
    /// The entity spawned by a report.
    /// </summary>
    [DataField("reportEntityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ReportEntityId = "Paper";
}
