using Content.Shared.Armor;
using Content.Shared.Atmos;
using Content.Shared.Chat;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Hypospray.Events;
using Content.Shared.Climbing.Events;
using Content.Shared.Contraband;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Explosion;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash;
using Content.Shared.Gravity;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Implants;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Nutrition;
using Content.Shared.Overlays;
using Content.Shared.Projectiles;
using Content.Shared.Radio;
using Content.Shared.Slippery;
using Content.Shared.Standing;
using Content.Shared.Strip.Components;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable;
using Content.Shared.Zombies;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<InventoryComponent, DamageModifyEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, ElectrocutionAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, SlipAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshMovementSpeedModifiersEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, BeforeStripEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, SeeIdentityAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, ModifyChangedTemperatureEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, GetDefaultRadioChannelEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshNameModifiersEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, TransformSpeakerNameEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, SelfBeforeHyposprayInjectsEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, TargetBeforeHyposprayInjectsEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, SelfBeforeGunShotEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, SelfBeforeClimbEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, CoefficientQueryEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, ZombificationResistanceQueryEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, IsEquippingTargetAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, IsUnequippingTargetAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, ChameleonControllerOutfitSelectedEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, BeforeEmoteEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, StoodEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, DownedEvent>(RelayInventoryEvent);

        // by-ref events
        SubscribeLocalEvent<InventoryComponent, RefreshFrictionModifiersEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, BeforeStaminaDamageEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, GetExplosionResistanceEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, IsWeightlessEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, GetSpeedModifierContactCapEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, GetSlowedOverSlipperyModifierEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, ModifySlowOnDamageSpeedEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, ExtinguishEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, ProjectileReflectAttemptEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, HitScanReflectAttemptEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, GetContrabandDetailsEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, FlashAttemptEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, WieldAttemptEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, UnwieldAttemptEvent>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, IngestionAttemptEvent>(RefRelayInventoryEvent);

        // Eye/vision events
        SubscribeLocalEvent<InventoryComponent, CanSeeAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, GetEyeProtectionEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, GetBlurEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, SolutionScanEvent>(RelayInventoryEvent);

        // ComponentActivatedClientSystems
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ShowJobIconsComponent>>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ShowHealthBarsComponent>>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ShowHealthIconsComponent>>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ShowHungerIconsComponent>>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ShowThirstIconsComponent>>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ShowMindShieldIconsComponent>>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ShowSyndicateIconsComponent>>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ShowCriminalRecordIconsComponent>>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<BlackAndWhiteOverlayComponent>>(RefRelayInventoryEvent);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<NoirOverlayComponent>>(RefRelayInventoryEvent);

        SubscribeLocalEvent<InventoryComponent, GetVerbsEvent<EquipmentVerb>>(OnGetEquipmentVerbs);
        SubscribeLocalEvent<InventoryComponent, GetVerbsEvent<InnateVerb>>(OnGetInnateVerbs);

    }

    protected void RefRelayInventoryEvent<T>(EntityUid uid, InventoryComponent component, ref T args) where T : IInventoryRelayEvent
    {
        RelayEvent((uid, component), ref args);
    }

    protected void RelayInventoryEvent<T>(EntityUid uid, InventoryComponent component, T args) where T : IInventoryRelayEvent
    {
        RelayEvent((uid, component), args);
    }

    public void RelayEvent<T>(Entity<InventoryComponent> inventory, ref T args) where T : IInventoryRelayEvent
    {
        if (args.TargetSlots == SlotFlags.NONE)
            return;

        // this copies the by-ref event if it is a struct
        var ev = new InventoryRelayedEvent<T>(args, inventory.Owner);
        var enumerator = new InventorySlotEnumerator(inventory, args.TargetSlots);
        while (enumerator.NextItem(out var item))
        {
            RaiseLocalEvent(item, ev);
        }

        // and now we copy it back
        args = ev.Args;
    }

    public void RelayEvent<T>(Entity<InventoryComponent> inventory, T args) where T : IInventoryRelayEvent
    {
        if (args.TargetSlots == SlotFlags.NONE)
            return;

        var ev = new InventoryRelayedEvent<T>(args, inventory.Owner);
        var enumerator = new InventorySlotEnumerator(inventory, args.TargetSlots);
        while (enumerator.NextItem(out var item))
        {
            RaiseLocalEvent(item, ev);
        }
    }

    private void OnGetEquipmentVerbs(EntityUid uid, InventoryComponent component, GetVerbsEvent<EquipmentVerb> args)
    {
        // Automatically relay stripping related verbs to all equipped clothing.
        var ev = new InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>(args, uid);
        var enumerator = new InventorySlotEnumerator(component);
        while (enumerator.NextItem(out var item, out var slotDef))
        {
            if (!_strippable.IsStripHidden(slotDef, args.User) || args.User == uid)
                RaiseLocalEvent(item, ev);
        }
    }

    private void OnGetInnateVerbs(EntityUid uid, InventoryComponent component, GetVerbsEvent<InnateVerb> args)
    {
        // Automatically relay stripping related verbs to all equipped clothing.
        var ev = new InventoryRelayedEvent<GetVerbsEvent<InnateVerb>>(args, uid);
        var enumerator = new InventorySlotEnumerator(component, SlotFlags.WITHOUT_POCKET);
        while (enumerator.NextItem(out var item))
        {
            RaiseLocalEvent(item, ev);
        }
    }

}

/// <summary>
///     Event wrapper for relayed events.
/// </summary>
/// <remarks>
///      This avoids nested inventory relays, and makes it easy to have certain events only handled by the initial
///      target entity. E.g. health based movement speed modifiers should not be handled by a hat, even if that hat
///      happens to be a dead mouse. Clothing that wishes to modify movement speed must subscribe to
///      InventoryRelayedEvent&lt;RefreshMovementSpeedModifiersEvent&gt;
/// </remarks>
public sealed class InventoryRelayedEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;

    public EntityUid Owner;

    public InventoryRelayedEvent(TEvent args, EntityUid owner)
    {
        Args = args;
        Owner = owner;
    }
}

public interface IClothingSlots
{
    SlotFlags Slots { get; }
}

/// <summary>
///     Events that should be relayed to inventory slots should implement this interface.
/// </summary>
public interface IInventoryRelayEvent
{
    /// <summary>
    ///     What inventory slots should this event be relayed to, if any?
    /// </summary>
    /// <remarks>
    ///     In general you may want to exclude <see cref="SlotFlags.POCKET"/>, given that those items are not truly
    ///     "equipped" by the user.
    /// </remarks>
    public SlotFlags TargetSlots { get; }
}
