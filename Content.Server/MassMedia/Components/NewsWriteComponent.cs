using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.MassMedia.Components
{
    [RegisterComponent]
    public sealed partial class NewsWriteComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShareAvalible = false;

        [ViewVariables(VVAccess.ReadWrite), DataField("nextShare", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextShare;

        [ViewVariables(VVAccess.ReadWrite), DataField("shareCooldown")]
        public float ShareCooldown = 60f;

        [DataField("noAccessSound")]
        public SoundSpecifier NoAccessSound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");
        [DataField("confirmSound")]
        public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
    }
}
