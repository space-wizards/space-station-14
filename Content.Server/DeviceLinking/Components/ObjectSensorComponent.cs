using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// An anchored subfloor thing that detects entities that are on the same tile.
/// </summary>
[RegisterComponent, Access(typeof(ObjectSensorSystem))]
public sealed partial class ObjectSensorComponent : Component
{
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort1 = "ObjectSensor1Object";

    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort2 = "ObjectSensor2Objects";

    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort3 = "ObjectSensor3Objects";

    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort4OrMore = "ObjectSensor4OrMoreObjects";

    [DataField]
    public ProtoId<ToolQualityPrototype> CycleQuality = "Screwing";

    [DataField]
    public int Contacting;

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
