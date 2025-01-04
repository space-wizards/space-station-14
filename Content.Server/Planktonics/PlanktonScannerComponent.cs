namespace Content.Server.Plankton;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

[RegisterComponent]
public sealed partial class PlanktonScannerComponent : Component
{

    [DataField]
    public bool AnalysisMode = false;

    [DataField("planktonReportEntityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PlanktonReportEntityId = "Paper";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [DataField("planktonRewardEntityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PlanktonRewardEntityId = "ResearchDisk5000";

    [DataField("planktonAdvancedRewardEntityId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PlanktonAdvancedRewardEntityId = "ResearchDisk10000";
}
