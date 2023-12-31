using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Components;

/// <summary>
/// This is used for an object that can instantly create a machine upon having a tool applied to it.
/// </summary>
[RegisterComponent]
public sealed partial class FlatpackComponent : Component
{
    /// <summary>
    /// The tool quality that, upon used to interact with this object, will create the <see cref="Entity"/>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ToolQualityPrototype> QualityNeeded = "Pulsing";

    /// <summary>
    /// The entity that is spawned when this object is unpacked.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<EntityPrototype>? Entity;

    /// <summary>
    /// Sound effect played upon the object being unpacked.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier UnpackSound = new SoundPathSpecifier("/Audio/Effects/unwrap.ogg");
}
