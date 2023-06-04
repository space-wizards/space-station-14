using Content.Shared.Eye.Blinding.Components;
using Robust.Client.Graphics;

namespace Content.Client.Eye.Components
{
    [RegisterComponent]
    public sealed class EyeTraitsComponent : Component
    {
        // Characters natural traits
        [DataField("nightVision"), ViewVariables(VVAccess.ReadWrite)]
        public NightVision? Night { get; set; } = new();
        [DataField("autoExpose"), ViewVariables(VVAccess.ReadWrite)]
        public AutoExpose? AutoExpose { get; set; } = new();

        // Current eye protection
        public EntityUid EyeProtectionUid = EntityUid.Invalid;
        public EyeProtectionComponent? EyeProtection = null;

        // Current Mask eye protection
        public EntityUid MaskProtectionUid = EntityUid.Invalid;
        public EyeProtectionComponent? MaskProtection = null;

        // If you want other stuff other than glasses to affect eyes, you'll need to combine them somehow?
    }
}
