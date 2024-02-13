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
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> OutputPort1 = "ObjectSensor1Object";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> OutputPort2 = "ObjectSensor2Objects";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> OutputPort3 = "ObjectSensor3Objects";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> OutputPort4OrMore = "ObjectSensor4OrMoreObjects";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ToolQualityPrototype> CycleQuality = "Screwing";

    [ViewVariables]
    public int Contacting = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ObjectSensorMode Mode = ObjectSensorMode.Living;

    [DataField]
    public SoundSpecifier CycleSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}

public enum ObjectSensorMode
{
    Living,
    Items,
    All
}
