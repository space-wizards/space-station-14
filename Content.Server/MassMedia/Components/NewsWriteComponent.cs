using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.MassMedia.Components
{
    [RegisterComponent]
    public sealed class NewsWriteComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShareAvalible = false;

        [ViewVariables(VVAccess.ReadWrite), DataField("nextShare", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextShare;

        [ViewVariables(VVAccess.ReadWrite), DataField("shareCooldown")]
        public float ShareCooldown = 60f;
    }
}
