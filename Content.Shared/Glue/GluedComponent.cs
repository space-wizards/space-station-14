using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Glue;

[RegisterComponent]
public sealed class GluedComponent : Component
{
    /// <summary>
    /// Reverts name to before prefix event (essentially removes prefix).
    /// </summary>
    [DataField("beforeGluedEntityName"), ViewVariables(VVAccess.ReadOnly)]
    public string BeforeGluedEntityName = String.Empty;

    /// <summary>
    /// Sound made when glue applied.
    /// </summary>
    [DataField("squeeze")]
    public SoundSpecifier Squeeze = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");

    /// <summary>
    /// Timings for glue duration and removal.
    /// </summary>
    [DataField("nextGlueTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextGlueTime;

    [DataField("glueTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan GlueTime = TimeSpan.Zero;

    [DataField("glueCooldown")]
    public TimeSpan GlueCooldown = TimeSpan.FromSeconds(20);


    /// <summary>
    /// Bools which control timings and when to apply the glue effect.
    /// </summary>
    public bool GlueBroken = false;

    [DataField("glued")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Glued = false;
}
