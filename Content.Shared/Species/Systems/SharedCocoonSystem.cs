using System.Linq;
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Containers.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Content.Shared.Rotation;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Destructible;
using Content.Shared.Interaction.Events;
using Content.Shared.Gibbing;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Species.Arachnid;

public abstract class SharedCocoonSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly HungerSystem _hunger = default!;
    [Dependency] protected readonly SharedPopupSystem _popups = default!;
    [Dependency] protected readonly ActionBlockerSystem _blocker = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly INetManager _netMan = default!;
    [Dependency] protected readonly SharedHandsSystem _hands = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly AlertsSystem _alerts = default!;
    [Dependency] protected readonly StandingStateSystem _standing = default!;
    [Dependency] protected readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] protected readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected const string CocoonContainerId = "cocoon_victim";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CocoonerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CocoonerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CocoonerComponent, WrapActionEvent>(OnWrapAction);
        SubscribeLocalEvent<CocoonerComponent, WrapDoAfterEvent>(OnWrapDoAfter);
        SubscribeLocalEvent<CocoonedComponent, RemoveCocoonAlertEvent>(OnRemoveCocoonAlert);
        SubscribeLocalEvent<CocoonContainerComponent, BreakFreeDoAfterEvent>(OnBreakFreeDoAfter);
        SubscribeLocalEvent<CocoonedComponent, AttackAttemptEvent>(OnCocoonedAttackAttempt);
        SubscribeLocalEvent<CocoonedComponent, GibbedBeforeDeletionEvent>(OnCocoonedVictimGibbed);
        SubscribeLocalEvent<CocoonContainerComponent, ComponentShutdown>(OnCocoonContainerShutdown);
        SubscribeLocalEvent<CocoonContainerComponent, DestructionEventArgs>(OnCocoonContainerDestroyed);
        SubscribeLocalEvent<CocoonContainerComponent, DamageChangedEvent>(OnCocoonContainerDamage);
        SubscribeLocalEvent<CocoonContainerComponent, UnwrapDoAfterEvent>(OnUnwrapDoAfter);
        SubscribeLocalEvent<ContainerManagerComponent, ComponentShutdown>(OnCocoonContainerManagerShutdown);
        SubscribeLocalEvent<CocoonContainerComponent, GetVerbsEvent<InteractionVerb>>(OnGetUnwrapVerb);
    }

    /// <summary>
    /// Server-only operations for wrap DoAfter completion (admin logs, etc.)
    /// </summary>
    protected abstract void OnWrapDoAfterServer(EntityUid performer, EntityUid target, EntityUid cocoonContainer);

    /// <summary>
    /// Server-only operations for wrap action (admin logs, etc.)
    /// </summary>
    protected abstract void OnWrapActionServer(EntityUid user, EntityUid target);

    /// <summary>
    /// Apply effects to victim after insertion
    /// </summary>
    protected abstract void OnWrapDoAfterSetupVictimEffects(EntityUid victim);

    /// <summary>
    /// Server-only: Remove MumbleAccentComponent from victim
    /// </summary>
    protected abstract void OnCocoonContainerShutdownRemoveMumbleAccent(EntityUid victim);

    private void OnMapInit(EntityUid uid, CocoonerComponent component, MapInitEvent args)
    {
        // Check if the action prototype exists (test-safe)
        if (component.WrapAction != default && !_protoManager.TryIndex<EntityPrototype>(component.WrapAction, out _))
            return;

        _actions.AddAction(uid, ref component.ActionEntity, component.WrapAction, container: uid);
    }

    private void OnShutdown(EntityUid uid, CocoonerComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnWrapAction(EntityUid uid, CocoonerComponent component, ref WrapActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        var target = args.Target;

        if (target == user)
            return;

        if (!HasComp<HumanoidProfileComponent>(target))
        {
            _popups.PopupClient(Loc.GetString("arachnid-wrap-invalid-target"), user, user);
            return;
        }

        // Check if target is already in a cocoon container
        if (_container.TryGetContainingContainer(target, out var existingContainer) &&
            HasComp<CocoonContainerComponent>(existingContainer.Owner))
        {
            _popups.PopupClient(Loc.GetString("arachnid-wrap-already"), user, user);
            return;
        }

        if (!_blocker.CanInteract(user, target))
            return;

        // Check if entity has enough hunger to perform the action
        if (TryComp<Content.Shared.Nutrition.Components.HungerComponent>(user, out var hungerComp))
        {
            var currentHunger = _hunger.GetHunger(hungerComp);
            if (currentHunger < component.HungerCost)
            {
                _popups.PopupClient(Loc.GetString("arachnid-wrap-failure-hunger"), user, user);
                return;
            }
        }

        // Only require hands if the entity has hands (spiders don't have hands)
        var needHand = HasComp<HandsComponent>(user);

        var wrapTime = component.WrapDuration;
        // Reduce DoAfter time if target is stunned, asleep, critical, or dead
        if (HasComp<StunnedComponent>(target) || HasComp<SleepingComponent>(target) || _mobState.IsCritical(target) || _mobState.IsDead(target))
            wrapTime = component.WrapDuration_Short;

        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(wrapTime), new WrapDoAfterEvent(), user, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = needHand,
            DistanceThreshold = component.WrapRange,
            CancelDuplicate = true,
            BlockDuplicate = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        // Server-only operations (admin logs, etc.)
        if (!_netMan.IsClient)
        {
            OnWrapActionServer(user, target);
        }

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_start.ogg"), target, user);

        _popups.PopupClient(Loc.GetString("arachnid-wrap-start-user", ("target", target)), user, user);
        _popups.PopupEntity(Loc.GetString("arachnid-wrap-start-target", ("user", user)), target, target, PopupType.LargeCaution);

        args.Handled = true;
    }

    protected virtual void OnWrapDoAfter(EntityUid uid, CocoonerComponent component, ref WrapDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var performer = args.User;
        var target = args.Args.Target.Value;

        if (!HasComp<HumanoidProfileComponent>(target))
            return;

        // Check if target is already cocooned (prevent duplicate spawns during prediction rerolls)
        // Check both CocoonedComponent and if target is in a cocoon container
        if (HasComp<CocoonedComponent>(target) ||
            (_container.TryGetContainingContainer(target, out var existingContainer) &&
             HasComp<CocoonContainerComponent>(existingContainer.Owner)))
        {
            _popups.PopupPredicted(Loc.GetString("arachnid-wrap-already"), target, performer);
            args.Handled = true;
            return;
        }

        if (!_blocker.CanInteract(performer, target))
            return;

        // Only consume hunger if the entity has a HungerComponent
        if (TryComp<Content.Shared.Nutrition.Components.HungerComponent>(performer, out var hunger))
        {
            _hunger.ModifyHunger(performer, -component.HungerCost);
        }

        // Only spawn on server to avoid metadata mismatch errors with PredictedSpawnAtPosition
        if (_netMan.IsClient)
        {
            args.Handled = true;
            return;
        }

        // Spawn cocoon container at target's position (server-only)
        var xform = Transform(target);
        var cocoonContainer = SpawnAtPosition("CocoonContainer", xform.Coordinates);

        // Set up the container component
        if (!TryComp<CocoonContainerComponent>(cocoonContainer, out var cocoonComp))
        {
            Log.Error("CocoonContainer spawned without CocoonContainerComponent!");
            Del(cocoonContainer);
            return;
        }

        cocoonComp.Victim = target;

        // Add BloodstreamProxyContainerComponent to allow syringes/hyposprays to inject into the victim
        var proxyComp = EnsureComp<BloodstreamProxyContainerComponent>(cocoonContainer);
        proxyComp.ContainerId = CocoonContainerId;

        Dirty(cocoonContainer, cocoonComp);

        // Drop all items from victim's hands before inserting
        if (TryComp<HandsComponent>(target, out var handsComponent))
        {
            var freeHands = 0;
            foreach (var hand in _hands.EnumerateHands((target, handsComponent)))
            {
                if (!_hands.TryGetHeldItem((target, handsComponent), hand, out var held))
                {
                    freeHands++;
                    continue;
                }

                // Is this entity removable? (it might be an existing handcuff blocker)
                if (HasComp<UnremoveableComponent>(held))
                    continue;

                _hands.DoDrop((target, handsComponent), hand, true);
                freeHands++;
                if (freeHands == 2)
                    break;
            }
        }

        // Ensure the container exists (it should already be created by the prototype, but create it if missing)
        var victimContainer = _container.EnsureContainer<Container>(cocoonContainer, CocoonContainerId);

        // Insert victim into container
        if (!_container.Insert(target, victimContainer))
        {
            Log.Error($"Failed to insert {target} into cocoon container {cocoonContainer}");
            Del(cocoonContainer);
            return;
        }

        // Check if victim was standing before applying effects (SetupVictimEffects forces them down)
        var victimWasStanding = !_standing.IsDown(target);

        // Apply effects to victim after insertion (ComponentStartup may have fired before victim was set)
        OnWrapDoAfterSetupVictimEffects(target);

        // Add CocoonedComponent to victim and show alert
        var cocoonedComp = EnsureComp<CocoonedComponent>(target);
        _alerts.ShowAlert(target, cocoonedComp.CocoonedAlert);

        // Prevent victim from grabbing anything by spawning virtual items in both hands
        // Unlike cuffs which require both hands to be free, we apply even if one hand is occupied or missing
        if (TryComp<HandsComponent>(target, out var victimHands))
        {
            // Try to spawn virtual item in first available hand
            if (_virtualItem.TrySpawnVirtualItemInHand(cocoonContainer, target, out var virtItem1))
            {
                EnsureComp<UnremoveableComponent>(virtItem1.Value);
            }

            // Try to spawn virtual item in second available hand
            if (_virtualItem.TrySpawnVirtualItemInHand(cocoonContainer, target, out var virtItem2))
            {
                EnsureComp<UnremoveableComponent>(virtItem2.Value);
            }
        }

        // Set rotation state on server so it replicates to all clients via AppearanceComponent
        // If victim was standing, set to horizontal (will animate). If already down, set to horizontal immediately.
        _appearance.SetData(cocoonContainer, RotationVisuals.RotationState, RotationState.Horizontal);

        // Send networked event to client for additional client-side visual handling (scale adjustment, etc.)
        RaiseNetworkEvent(new CocoonRotationAnimationEvent(GetNetEntity(cocoonContainer), victimWasStanding));

        // Play sound and show popups when entity is actually spawned and visible
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_end.ogg"), cocoonContainer);
        _popups.PopupEntity(Loc.GetString("arachnid-wrap-complete-user", ("target", target)), cocoonContainer, performer);
        _popups.PopupEntity(Loc.GetString("arachnid-wrap-complete-target"), cocoonContainer, target, PopupType.LargeCaution);

        OnWrapDoAfterServer(performer, target, cocoonContainer);

        args.Handled = true;
    }



    private void OnRemoveCocoonAlert(Entity<CocoonedComponent> ent, ref RemoveCocoonAlertEvent args)
    {
        if (args.Handled)
            return;

        var victim = ent.Owner;

        // Find the cocoon container that contains this victim
        if (!_container.TryGetContainingContainer(victim, out var container) ||
            !TryComp<CocoonContainerComponent>(container.Owner, out var cocoonComp))
        {
            args.Handled = true;
            return;
        }

        var cocoonContainer = container.Owner;

        // Attach DoAfter to cocoon container (visible) instead of victim (hidden in container)
        // This makes the progress bar visible to all players
        // Track cocoon container movement (victim can't move independently)
        // The event will be raised on the cocoon container, which has CocoonContainerComponent
        var doAfter = new DoAfterArgs(
            EntityManager,
            cocoonContainer,
            TimeSpan.FromSeconds(cocoonComp.BreakFreeDuration),
            new BreakFreeDoAfterEvent(),
            cocoonContainer) // EventTarget - event will be raised on cocoon container
        {
            BreakOnMove = true, // Track cocoon container movement (victim is inside, can't move independently)
            BreakOnDamage = true,
            CancelDuplicate = true,
            BlockDuplicate = true,
            DistanceThreshold = null, // Victim is inside container, so skip range check
            RequireCanInteract = false, // Victim is blocked (cocooned), so they can't interact normally
            Hidden = false, // Make progress bar visible to all players
            Broadcast = true, // Broadcast the event so all clients receive it
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            args.Handled = true;
            return;
        }

        _popups.PopupClient(Loc.GetString("arachnid-break-free-start"), cocoonContainer, victim);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_takeoff.ogg"), cocoonContainer, victim);

        args.Handled = true;
    }

    protected virtual void OnCocoonContainerShutdown(EntityUid uid, CocoonContainerComponent component, ComponentShutdown args)
    {
        if (component.Victim == null || !Exists(component.Victim.Value))
            return;

        var victim = component.Victim.Value;

        // Remove virtual items from victim's hands
        _virtualItem.DeleteInHandsMatching(victim, uid);

        // Remove CocoonedComponent and clear alert
        if (TryComp<CocoonedComponent>(victim, out var cocoonedComp))
        {
            _alerts.ClearAlert(victim, cocoonedComp.CocoonedAlert);
            RemComp<CocoonedComponent>(victim);
        }

        // Remove effects from victim
        if (HasComp<BlockMovementComponent>(victim))
        {
            RemComp<BlockMovementComponent>(victim);
            // Update movement blockers immediately after removal
            _blocker.UpdateCanMove(victim);
        }

        // Remove server-only MumbleAccentComponent
        OnCocoonContainerShutdownRemoveMumbleAccent(victim);

        if (HasComp<TemporaryBlindnessComponent>(victim))
        {
            RemComp<TemporaryBlindnessComponent>(victim);
        }

        // Ensure the character is downed
        if (TryComp<StandingStateComponent>(victim, out var standingState))
        {
            // Ensure they're downed (they should already be from when cocooned)
            if (standingState.Standing)
            {
                _standing.Down(victim, playSound: false, force: true);
            }

            var wasNew = EnsureComp<KnockedDownComponent>(victim, out var knockedDown);

            // Configure knockdown: prevent auto-standing, show alert, allow immediate stand-up
            _stun.SetAutoStand((victim, knockedDown), false);
            _alerts.ShowAlert(victim, SharedStunSystem.KnockdownAlert);

            if (wasNew)
            {
                _stun.SetKnockdownTime((victim, knockedDown), TimeSpan.Zero);
            }
        }
    }

    protected virtual void OnCocoonContainerDestroyed(EntityUid uid, CocoonContainerComponent component, DestructionEventArgs args)
    {
        // Play sound and show popup directly when cocoon is destroyed by damage
        var coords = Transform(uid).Coordinates;
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_breakout.ogg"), coords);

        // Show popup to victim
        if (component.Victim != null && Exists(component.Victim.Value))
        {
            _popups.PopupEntity(Loc.GetString("arachnid-cocoon-broken"), uid, component.Victim.Value, PopupType.LargeCaution);
        }
    }

    protected virtual void OnCocoonContainerDamage(EntityUid uid, CocoonContainerComponent component, DamageChangedEvent args)
    {
        // Only process if damage was actually increased
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        var damageDelta = args.DamageDelta;
        var totalDamage = damageDelta.GetTotal().Float();
        if (totalDamage <= 0)
            return;

        // Calculate the absorbed damage per damage type
        var absorbedDamageSpec = new DamageSpecifier();
        foreach (var (key, value) in damageDelta.DamageDict)
        {
            absorbedDamageSpec.DamageDict[key] = value * component.AbsorbPercentage;
        }

        // Calculate victim damage: total damage - absorbed damage
        var victimDamage = damageDelta - absorbedDamageSpec;

        // Modify the DamageableComponent's damage to only include the absorbed portion
        // Current damage already includes the full delta, so we need to:
        // 1. Get current damage
        // 2. Subtract the delta that was just added
        // 3. Add only the absorbed portion
        var currentDamage = args.Damageable.Damage;

        // Set damage to: current - delta + absorbed (using DamageSpecifier operators)
        var newDamage = currentDamage - damageDelta + absorbedDamageSpec;

        // Set the damage to only the absorbed portion
        _damageable.SetDamage(uid, newDamage);

        // Pass the reduced damage to the victim inside
        if (component.Victim != null && Exists(component.Victim.Value))
        {
            // Apply the reduced damage directly to the victim
            _damageable.TryChangeDamage(component.Victim.Value, victimDamage, origin: args.Origin);
        }

        // The DestructibleComponent will automatically trigger when damage reaches the threshold
    }

    /// <summary>
    /// Empties the cocoon container, removing all entities to prevent them from being deleted.
    /// </summary>
    protected void EmptyCocoonContainer(EntityUid uid)
    {
        if (!_container.TryGetContainer(uid, CocoonContainerId, out var container))
            return;

        var coords = Transform(uid).Coordinates;
        var containedEntities = container.ContainedEntities.ToList();
        foreach (var entity in containedEntities)
        {
            if (!Deleted(entity) && container.Contains(entity))
            {
                _container.Remove(entity, container, destination: coords, force: true);
            }
        }
    }

    protected virtual void OnUnwrapDoAfter(EntityUid uid, CocoonContainerComponent component, ref UnwrapDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        // On client, don't manipulate entities to avoid jittering
        if (_netMan.IsClient)
        {
            // Don't predict sound/popup - they will play on server when entity is actually destroyed
            args.Handled = true;
            return;
        }

        // Get coordinates before entity is deleted
        var coords = Transform(uid).Coordinates;

        // Play sound for everyone and show popups directly before deletion (server-only)
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_breakout.ogg"), coords);

        if (component.Victim != null && Exists(component.Victim.Value))
        {
            var victim = component.Victim.Value;
            _popups.PopupCoordinates(Loc.GetString("arachnid-unwrap-user", ("target", victim)), coords, args.User);
            _popups.PopupCoordinates(Loc.GetString("arachnid-unwrap-target", ("user", args.User)), coords, victim);
        }

        // Empty the container before deletion
        // OnCocoonContainerManagerShutdown handles this for destructible-triggered destruction,
        // but we need to do it manually here since Del() queues deletion and shutdowns happen later
        EmptyCocoonContainer(uid);

        // Delete the container - OnCocoonContainerShutdown will handle victim cleanup
        QueueDel(uid);

        args.Handled = true;
    }

    protected virtual void OnBreakFreeDoAfter(Entity<CocoonContainerComponent> ent, ref BreakFreeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var cocoonContainer = ent.Owner;
        var component = ent.Comp;

        // Get the victim from the container component
        if (component.Victim == null || !Exists(component.Victim.Value))
        {
            args.Handled = true;
            return;
        }

        var victim = component.Victim.Value;

        // On client, don't manipulate entities to avoid jittering
        if (_netMan.IsClient)
        {
            // Don't predict sound/popup - they will play on server when entity is actually destroyed
            args.Handled = true;
            return;
        }

        // Server-side: perform actual entity manipulation
        // Get coordinates before entity is deleted
        var coords = Transform(cocoonContainer).Coordinates;

        // Play sound for everyone (including victim) and show popup directly before deletion
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_breakout.ogg"), coords);
        _popups.PopupCoordinates(Loc.GetString("arachnid-break-free-complete"), coords, victim, PopupType.Large);

        // Empty the container before deletion
        EmptyCocoonContainer(cocoonContainer);

        // Delete the container - OnCocoonContainerShutdown will handle victim cleanup
        QueueDel(cocoonContainer);

        args.Handled = true;
    }

    protected virtual void OnCocoonContainerManagerShutdown(EntityUid uid, ContainerManagerComponent component, ComponentShutdown args)
    {
        // Only handle cocoon containers
        if (!HasComp<CocoonContainerComponent>(uid))
            return;

        EmptyCocoonContainer(uid);
    }

    private void OnGetUnwrapVerb(EntityUid uid, CocoonContainerComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        // Must be in range, must be able to interact
        if (!args.CanAccess)
            return;

        if (!args.CanInteract)
            return;

        // Unwrapping verb
        var unwrapVerb = new InteractionVerb
        {
            Text = Loc.GetString("arachnid-unwrap-verb", ("target", component.Victim ?? uid)),
            Priority = 10,
            Act = () =>
            {
                if (!_blocker.CanInteract(args.User, uid))
                    return;

                // Only require hands if the entity has hands
                var needHand = HasComp<HandsComponent>(args.User);

                var doAfter = new DoAfterArgs(
                    EntityManager,
                    args.User,
                    TimeSpan.FromSeconds(component.UnwrapDuration),
                    new UnwrapDoAfterEvent(),
                    uid,
                    uid)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = needHand,
                    DistanceThreshold = 1.5f,
                    CancelDuplicate = true,
                    BlockDuplicate = true,
                };

                if (!_doAfter.TryStartDoAfter(doAfter))
                    return;

                _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_takeoff.ogg"), uid, args.User);

                if (component.Victim != null && Exists(component.Victim.Value))
                {
                    var victim = component.Victim.Value;
                    _popups.PopupClient(Loc.GetString("arachnid-unwrap-start-user", ("target", victim)), uid, args.User);
                    _popups.PopupEntity(Loc.GetString("arachnid-unwrap-start-target", ("user", args.User)), uid, victim);
                }
                else
                {
                    _popups.PopupClient(Loc.GetString("arachnid-unwrap-start-user-empty"), uid, args.User);
                }
            }
        };
        args.Verbs.Add(unwrapVerb);
    }

    protected virtual void OnCocoonedAttackAttempt(Entity<CocoonedComponent> ent, ref AttackAttemptEvent args)
    {
        // Prevent cocooned victims from attacking at all (similar to handcuffs)
        args.Cancel();
    }

    protected virtual void OnCocoonedVictimGibbed(Entity<CocoonedComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        // Find the cocoon container that contains this victim
        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        if (!TryComp<CocoonContainerComponent>(container.Owner, out var cocoonComp))
            return;

        // Move all gibbed parts into the cocoon container
        if (!_container.TryGetContainer(container.Owner, CocoonContainerId, out var victimContainer))
            return;

        foreach (var gibbedPart in args.Giblets)
        {
            if (Deleted(gibbedPart))
                continue;

            // If it's a transform child of the cocoon, detach it first
            var gibbedTransform = Transform(gibbedPart);
            if (gibbedTransform.ParentUid == container.Owner)
            {
                _transform.DetachEntity(gibbedPart, gibbedTransform);
            }

            // Insert into the container
            if (!victimContainer.Contains(gibbedPart))
            {
                _container.Insert(gibbedPart, victimContainer, force: true);
            }
        }
    }
}

public sealed partial class WrapActionEvent : EntityTargetActionEvent
{
}

public sealed partial class UnwrapActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class WrapDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class UnwrapDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class BreakFreeDoAfterEvent : SimpleDoAfterEvent
{
}
