using Content.Server.MassMedia.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.MassMedia.Components;


[RegisterComponent]
[Access(typeof(NewsSystem))]
public sealed partial class NewsWriterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("publishEnabled")]
    public bool PublishEnabled;

    [ViewVariables(VVAccess.ReadWrite), DataField("nextPublish", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPublish;

    [ViewVariables(VVAccess.ReadWrite), DataField("publishCooldown")]
    public float PublishCooldown = 60f;

    [DataField("noAccessSound")]
    public SoundSpecifier NoAccessSound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");
    [DataField("confirmSound")]
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}
