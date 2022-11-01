using Content.Shared.MachineLinking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class AnalysisConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AnalyzerEntity;

    [DataField("linkingPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
    public readonly string LinkingPort = "ArtifactAnalyzerSender";

    [DataField("destroySound")]
    public SoundSpecifier DestroySound = new SoundPathSpecifier("/Audio/Effects/radpulse11.ogg");
}
