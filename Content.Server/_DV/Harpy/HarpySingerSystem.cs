using Content.Server.Instruments;
using Content.Server.Speech.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.ForceSay;
using Content.Shared._DV.Harpy;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Content.Shared.Zombies;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.Harpy;

public sealed class HarpySingerSystem : EntitySystem
{
    [Dependency] private readonly InstrumentSystem _instrument = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        // imp edits - InstrumentComponent > HarpySingerComponent
        SubscribeLocalEvent<HarpySingerComponent, MobStateChangedEvent>(OnMobStateChangedEvent);
        SubscribeLocalEvent<HarpySingerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<HarpySingerComponent, EntityZombifiedEvent>(OnZombified);
        SubscribeLocalEvent<HarpySingerComponent, KnockedDownEvent>(OnKnockedDown);
        SubscribeLocalEvent<HarpySingerComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<HarpySingerComponent, SleepStateChangedEvent>(OnSleep);
        SubscribeLocalEvent<HarpySingerComponent, StatusEffectAddedEvent>(OnStatusEffect);
        SubscribeLocalEvent<HarpySingerComponent, DamageChangedEvent>(OnDamageChanged);

        // This is intended to intercept the UI event and stop the MIDI UI from opening if the
        // singer is unable to sing. Thus it needs to run before the ActivatableUISystem.
        SubscribeLocalEvent<HarpySingerComponent, OpenUiActionEvent>(OnInstrumentOpen, before: new[] { typeof(ActivatableUISystem) });
    }

    private void OnEquip(Entity<HarpySingerComponent> ent, ref GotEquippedEvent args)
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

    private void OnMobStateChangedEvent(Entity<HarpySingerComponent> ent, ref MobStateChangedEvent args) // Imp - InstrumentComponent > HarpySingerComponent
    {
        if (args.NewMobState is MobState.Critical or MobState.Dead)
            CloseMidiUi(args.Target);
    }

    private void OnZombified(Entity<HarpySingerComponent> ent, ref EntityZombifiedEvent args)
    {
        CloseMidiUi(args.Target);
    }

    private void OnKnockedDown(Entity<HarpySingerComponent> ent, ref KnockedDownEvent args) // Imp - InstrumentComponent > HarpySingerComponent
    {
        CloseMidiUi(ent);
    }

    private void OnStunned(Entity<HarpySingerComponent> ent, ref StunnedEvent args) // Imp - InstrumentComponent > HarpySingerComponent
    {
        CloseMidiUi(ent);
    }

    private void OnSleep(Entity<HarpySingerComponent> ent, ref SleepStateChangedEvent args) // Imp - InstrumentComponent > HarpySingerComponent
    {
        if (args.FellAsleep)
            CloseMidiUi(ent);
    }

    private void OnStatusEffect(Entity<HarpySingerComponent> ent, ref StatusEffectAddedEvent args) // Imp - InstrumentComponent > HarpySingerComponent
    {
        if (args.Key == "Muted")
            CloseMidiUi(ent);
    }

    /// <summary>
    /// Almost a copy of Content.Server.Damage.ForceSay.DamageForceSaySystem.OnDamageChanged.
    /// Done so because DamageForceSaySystem doesn't output an event, and my understanding is
    /// that we don't want to change upstream code more than necessary to avoid merge conflicts
    /// and maintenance overhead. It still reuses the values from DamageForceSayComponent, so
    /// any tweaks to that will keep ForceSay consistent with singing interruptions.
    /// </summary>
    private void OnDamageChanged(Entity<HarpySingerComponent> ent, ref DamageChangedEvent args) // Imp - InstrumentComponent > HarpySingerComponent
    {
        if (!TryComp<DamageForceSayComponent>(ent, out var component) ||
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
            CloseMidiUi(ent);
    }

    /// <summary>
    /// Closes the MIDI UI if it is open.
    /// </summary>
    private void CloseMidiUi(EntityUid uid)
    {
        if (HasComp<ActiveInstrumentComponent>(uid) && HasComp<ActorComponent>(uid))
        {
            _instrument.ToggleInstrumentUi(uid, uid);
        }
    }

    /// <summary>
    /// Prevent the player from opening the MIDI UI under some circumstances.
    /// </summary>
    private void OnInstrumentOpen(Entity<HarpySingerComponent> ent, ref OpenUiActionEvent args) // Imp - InstrumentComponent > HarpySingerComponent
    {
        // CanSpeak covers all reasons you can't talk, including being incapacitated
        // (crit/dead), asleep, or for any reason mute inclding glimmer or a mime's vow.
        var canNotSpeak = !_blocker.CanSpeak(ent);
        var zombified = TryComp<ZombieComponent>(ent, out var _);
        var muzzled = _inventorySystem.TryGetSlotEntity(ent, "mask", out var maskUid) &&
                      TryComp<AddAccentClothingComponent>(maskUid, out var accent) &&
                      accent.ReplacementPrototype == "mumble";

        // Set this event as handled when the singer should be incapable of singing in order
        // to stop the ActivatableUISystem event from opening the MIDI UI.
        args.Handled = canNotSpeak || muzzled || zombified;

        // Tell the user that they can not sing.
        if (args.Handled)
            _popupSystem.PopupEntity(Loc.GetString("no-sing-while-no-speak"), ent, ent, PopupType.Medium);
    }
}
