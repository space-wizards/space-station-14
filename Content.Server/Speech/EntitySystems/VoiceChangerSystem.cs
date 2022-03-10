using Content.Server.Speech.Components;
using Content.Shared.Inventory.Events;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class VoiceChangerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VoiceChangerComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<VoiceChangerComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnEquipped(EntityUid uid, VoiceChangerComponent component, GotEquippedEvent args)
        {
            if (args.SlotFlags != component.ActivationSlot)
                return;

            var voice = EnsureComp<VoiceChangerVoiceComponent>(args.Equipee);
            voice.voiceName = component.SetVoiceName;
            component.Equipped = true;
        }

        private void OnUnequipped(EntityUid uid, VoiceChangerComponent component, GotUnequippedEvent args)
        {
            // This probably needs to be more robust when there are other sources of this effect
            if (component.Equipped == true)
            {
                RemComp<VoiceChangerVoiceComponent>(args.Equipee);
                component.Equipped = false;
            }
        }
    }
}
