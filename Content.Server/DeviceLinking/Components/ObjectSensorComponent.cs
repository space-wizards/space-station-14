using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// An anchored object that detects entities that touch it.
/// </summary>
[RegisterComponent, Access(typeof(ObjectSensorSystem))]
public sealed partial class ObjectSensorComponent : Component
{
    /// <summary>
    ///    The source ports used for each of the outputs.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort1 = "ObjectSensor1Object";

    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort2 = "ObjectSensor2Objects";

    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort3 = "ObjectSensor3Objects";

    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort4OrMore = "ObjectSensor4OrMoreObjects";

    /// <summary>
    ///    How the mode is switched
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> CycleQuality = "Screwing";

    /// <summary>
    ///    How many entities are contacting it
    /// </summary>
    [DataField]
    public int Contacting;

    /// <summary>
    ///    What type of entities will be detected
    /// </summary>
    [DataField]
    public ObjectSensorMode Mode = ObjectSensorMode.Living;

    [DataField]
    public SoundSpecifier CycleSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}

public enum ObjectSensorMode : byte
{
    Living,
    Items,
    All
}
