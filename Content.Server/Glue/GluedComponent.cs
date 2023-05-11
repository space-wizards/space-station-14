using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;



namespace Content.Server.Glue;

[RegisterComponent]
public sealed class GluedComponent : Component
{
    [DataField("beforeGluedEntityName"), ViewVariables(VVAccess.ReadOnly)]
    public string BeforeGluedEntityName = String.Empty;

    [DataField("enabled")]
    public bool Enabled = true;

    [DataField("squeeze")]
    public SoundSpecifier Squeeze = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");

    [DataField("nextGlueTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextGlueTime;

    [DataField("glued")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Glued = false;

    [DataField("glueTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan GlueTime = TimeSpan.Zero;

    [DataField("glueCooldown")]
    public TimeSpan GlueCooldown = TimeSpan.FromSeconds(20);

    public bool GlueBroken = false;
}
