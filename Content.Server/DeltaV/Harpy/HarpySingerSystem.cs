using Content.Server.Instruments;
using Content.Server.Speech.Components;
using Content.Server.UserInterface;
using Content.Shared.Instruments;
using Content.Shared.Instruments.UI;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.ForceSay;
using Content.Shared.DeltaV.Harpy;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Content.Shared.Zombies;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.DeltaV.Harpy.Components;

namespace Content.Server.DeltaV.Harpy
{
    public sealed class HarpySingerSystem : EntitySystem
    {
        [Dependency] private readonly InstrumentSystem _instrument = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InstrumentComponent, MobStateChangedEvent>(OnMobStateChangedEvent);
            SubscribeLocalEvent<GotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<EntityZombifiedEvent>(OnZombified);
            SubscribeLocalEvent<InstrumentComponent, KnockedDownEvent>(OnKnockedDown);
            SubscribeLocalEvent<InstrumentComponent, StunnedEvent>(OnStunned);
            SubscribeLocalEvent<InstrumentComponent, SleepStateChangedEvent>(OnSleep);
            SubscribeLocalEvent<InstrumentComponent, StatusEffectAddedEvent>(OnStatusEffect);
            SubscribeLocalEvent<InstrumentComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<HarpySingerComponent, BoundUIClosedEvent>(OnBoundUIClosed);
            SubscribeLocalEvent<HarpySingerComponent, BoundUIOpenedEvent>(OnBoundUIOpened);

            // This is intended to intercept the UI event and stop the MIDI UI from opening if the
            // singer is unable to sing. Thus it needs to run before the ActivatableUISystem.
            SubscribeLocalEvent<HarpySingerComponent, OpenUiActionEvent>(OnInstrumentOpen, before: new[] { typeof(ActivatableUISystem) });
        }

        private void OnEquip(GotEquippedEvent args)
        {
            // Check if an item that makes the singer mumble is equipped to their face
            // (not their pockets!). As of writing, this should just be the muzzle.
            if (TryComp<AddAccentClothingComponent>(args.Equipment, out var accent) &&
                accent.ReplacementPrototype == "mumble" &&
                args.Slot == "mask")
            {
                CloseMidiUi(args.Equipee);
            }
        }

        private void OnMobStateChangedEvent(EntityUid uid, InstrumentComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState is MobState.Critical or MobState.Dead)
                CloseMidiUi(args.Target);
        }

        private void OnZombified(ref EntityZombifiedEvent args)
        {
            CloseMidiUi(args.Target);
        }

        private void OnKnockedDown(EntityUid uid, InstrumentComponent component, ref KnockedDownEvent args)
        {
            CloseMidiUi(uid);
        }

        private void OnStunned(EntityUid uid, InstrumentComponent component, ref StunnedEvent args)
        {
            CloseMidiUi(uid);
        }

        private void OnSleep(EntityUid uid, InstrumentComponent component, ref SleepStateChangedEvent args)
        {
            if (args.FellAsleep)
                CloseMidiUi(uid);
        }

        private void OnStatusEffect(EntityUid uid, InstrumentComponent component, StatusEffectAddedEvent args)
        {
            if (args.Key == "Muted")
                CloseMidiUi(uid);
        }

        /// <summary>
        /// Almost a copy of Content.Server.Damage.ForceSay.DamageForceSaySystem.OnDamageChanged.
        /// Done so because DamageForceSaySystem doesn't output an event, and my understanding is
        /// that we don't want to change upstream code more than necessary to avoid merge conflicts
        /// and maintenance overhead. It still reuses the values from DamageForceSayComponent, so
        /// any tweaks to that will keep ForceSay consistent with singing interruptions.
        /// </summary>
        private void OnDamageChanged(EntityUid uid, InstrumentComponent instrumentComponent, DamageChangedEvent args)
        {
            if (!TryComp<DamageForceSayComponent>(uid, out var component) ||
                args.DamageDelta == null ||
                !args.DamageIncreased ||
                args.DamageDelta.GetTotal() < component.DamageThreshold ||
                component.ValidDamageGroups == null)
                return;

            var totalApplicableDamage = FixedPoint2.Zero;
            foreach (var (group, value) in args.DamageDelta.GetDamagePerGroup(_prototype))
            {
                if (!component.ValidDamageGroups.Contains(group))
                    continue;

                totalApplicableDamage += value;
            }

            if (totalApplicableDamage >= component.DamageThreshold)
                CloseMidiUi(uid);
        }

        /// <summary>
        /// Closes the MIDI UI if it is open.
        /// </summary>
        private void CloseMidiUi(EntityUid uid)
        {
            if (HasComp<ActiveInstrumentComponent>(uid) &&
                TryComp<ActorComponent>(uid, out var actor))
            {
                var ent = actor.PlayerSession.AttachedEntity;
                if (ent == null)
                    return;
                _instrument.ToggleInstrumentUi(uid, ent.Value);

            }
        }

        /// <summary>
        /// Prevent the player from opening the MIDI UI under some circumstances.
        /// </summary>
        private void OnInstrumentOpen(EntityUid uid, HarpySingerComponent component, OpenUiActionEvent args)
        {
            // CanSpeak covers all reasons you can't talk, including being incapacitated
            // (crit/dead), asleep, or for any reason mute inclding glimmer or a mime's vow.
            var canNotSpeak = !_blocker.CanSpeak(uid);
            var zombified = TryComp<ZombieComponent>(uid, out var _);
            var muzzled = _inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
                TryComp<AddAccentClothingComponent>(maskUid, out var accent) &&
                accent.ReplacementPrototype == "mumble";

            // Set this event as handled when the singer should be incapable of singing in order
            // to stop the ActivatableUISystem event from opening the MIDI UI.
            args.Handled = canNotSpeak || muzzled || zombified;

            // Tell the user that they can not sing.
            if (args.Handled)
                _popupSystem.PopupEntity(Loc.GetString("no-sing-while-no-speak"), uid, uid, PopupType.Medium);
        }

        private void OnBoundUIClosed(EntityUid uid, HarpySingerComponent component, BoundUIClosedEvent args)
        {
            if (args.UiKey is not InstrumentUiKey)
                return;

            TryComp(uid, out AppearanceComponent? appearance);
            _appearance.SetData(uid, HarpyVisualLayers.Singing, SingingVisualLayer.False, appearance);
        }

        private void OnBoundUIOpened(EntityUid uid, HarpySingerComponent component, BoundUIOpenedEvent args)
        {
            if (args.UiKey is not InstrumentUiKey)
                return;

            TryComp(uid, out AppearanceComponent? appearance);
            _appearance.SetData(uid, HarpyVisualLayers.Singing, SingingVisualLayer.True, appearance);

        }
    }
}
