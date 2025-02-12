using Content.Server.Administration.Systems;

namespace Content.Server.Administration.Components;

[RegisterComponent, Access(typeof(BufferingSystem))]
public sealed partial class BufferingComponent : Component
{
    [DataField("minBufferTime")]
    public float MinimumBufferTime = 0.5f;
    [DataField("maxBufferTime")]
    public float MaximumBufferTime = 1.5f;
    [DataField("minTimeTilNextBuffer")]
    public float MinimumTimeTilNextBuffer = 10.0f;
    [DataField("maxTimeTilNextBuffer")]
    public float MaximumTimeTilNextBuffer = 120.0f;
    [DataField]
    public float TimeTilNextBuffer = 15.0f;
    [DataField("bufferingIcon")]
    public EntityUid? BufferingIcon = null;
    [DataField]
    public float BufferingTimer = 0.0f;
}
