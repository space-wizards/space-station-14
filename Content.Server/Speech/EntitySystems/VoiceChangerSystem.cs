using Content.Server.Speech.Components;
using Content.Server.UserInterface;
using Content.Server.Popups;
using Content.Shared.Speech.Systems;
using Content.Shared.Inventory.Events;
using Robust.Shared.Player;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class VoiceChangerSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VoiceChangerComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<VoiceChangerComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<VoiceChangerComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
            /// BUI
            SubscribeLocalEvent<VoiceChangerComponent, VoiceChangerNameChangedMessage>(OnNameChanged);
        }

        private void OnEquipped(EntityUid uid, VoiceChangerComponent component, GotEquippedEvent args)
        {
            if (args.SlotFlags != component.ActivationSlot)
                return;

            var voice = EnsureComp<VoiceChangerVoiceComponent>(args.Equipee);
            voice.VoiceName = component.SetVoiceName;
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

        private void OnUIOpenAttempt(EntityUid uid, VoiceChangerComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (component.Equipped)
            {
                _popupSystem.PopupEntity(Loc.GetString("voice-changer-unequip-first"), args.User, Filter.Entities(args.User));
                args.Cancel();
            }
        }

        private void OnNameChanged(EntityUid uid, VoiceChangerComponent component, VoiceChangerNameChangedMessage args)
        {
            component.SetVoiceName = args.Name;
        }
    }
}
