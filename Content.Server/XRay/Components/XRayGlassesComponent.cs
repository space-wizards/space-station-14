using Content.Shared.Inventory;

namespace Content.Server.Xray.Components
{
    [RegisterComponent]
    public sealed class XRayGlassesComponent : Component
    {
        [DataField("activationSlot")]
        public SlotFlags ActivationSlot = SlotFlags.EYES;

    }
}
