using Robust.Shared.Audio;

namespace Content.Server.Bodycamera
{
    [RegisterComponent]
    [Access(typeof(BodyCameraSystem))]
    public sealed partial class BodyCameraComponent : Component
    {
        [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled;

        [DataField("equipped"), ViewVariables(VVAccess.ReadWrite)]
        public bool Equipped;

        /// <summary>
        /// Power usage per second when enabled
        /// </summary>
        [DataField("wattage"), ViewVariables(VVAccess.ReadWrite)]
        public float Wattage = 0.6f; //Calculated to 10 minutes on a small cell

        [ViewVariables(VVAccess.ReadWrite), DataField("powerOnSound")]
        public SoundSpecifier? PowerOnSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

        [ViewVariables(VVAccess.ReadWrite), DataField("powerOffSound")]
        public SoundSpecifier? PowerOffSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");
    }
}
