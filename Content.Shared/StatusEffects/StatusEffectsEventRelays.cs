using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.StatusEffects.Components;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffects;

public sealed partial class StatusEffectsRelays : SharedStatusEffectsSystem
{
    public void InitializeRelays()
    {
        SubscribeLocalEvent<StatusEffectsComponent, MeleeHitEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, BeforeDamageChangedEvent>(RefRelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, DamageModifyEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, DamageChangedEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, GetStatusIconsEvent>(RefRelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, StandAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, ComponentGetState>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, ComponentHandleState>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, InteractHandEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, TileFrictionEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, ComponentStartup>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, ComponentShutdown>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, ChangeDirectionAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, UpdateCanMoveEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, InteractionAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, UseAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, ThrowAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, DropAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, AttackAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, PickupAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, IsEquippingAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, IsUnequippingAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, MobStateChangedEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, RefreshMovementSpeedModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, ComponentGetState>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, ComponentHandleState>(RelayEvent);
    }
}
