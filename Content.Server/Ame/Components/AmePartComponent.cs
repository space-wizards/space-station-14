using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Ame.Components;

/// <summary>
/// Packaged AME machinery that can be deployed to construct an AME.
/// </summary>
[RegisterComponent]
public sealed partial class AmePartComponent : Component
{
    /// <summary>
    /// The sound played when the AME shielding is unpacked.
    /// </summary>
    [DataField("unwrapSound")]
    public SoundSpecifier UnwrapSound = new SoundPathSpecifier("/Audio/Effects/unwrap.ogg");

    /// <summary>
    /// The tool quality required to deploy the packaged AME shielding.
    /// </summary>
    [DataField("qualityNeeded", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string QualityNeeded = "Pulsing";
}
