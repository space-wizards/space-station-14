 using Content.Shared.Inventory;

namespace Content.Server.Speech.Components
{
    // This is for the physical voice changer device, look at VoiceChangerVoice to use with other sources of the effect
    [RegisterComponent]
    public sealed class VoiceChangerComponent : Component
    {
        [DataField("activationSlot")]
        public SlotFlags ActivationSlot = SlotFlags.MASK;
        public string SetVoiceName = "Bane";

        public bool Equipped = false;
    }
}
