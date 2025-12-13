// SPDX-FileCopyrightText: 2025 Drywink <43855731+Drywink@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Rotation;
using Content.Shared.Species.Arachnid;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
using Content.Shared.Destructible;
using Content.Shared.Gibbing.Events;
using Content.Server.Body.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Species.Arachnid;

public sealed class CocoonSystem : SharedCocoonSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private const string CocoonContainerId = "cocoon_victim";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CocoonerComponent, WrapActionEvent>(OnWrapAction);
        SubscribeLocalEvent<CocoonerComponent, WrapDoAfterEvent>(OnWrapDoAfter);

        SubscribeLocalEvent<CocoonContainerComponent, ComponentShutdown>(OnCocoonContainerShutdown);
        SubscribeLocalEvent<CocoonContainerComponent, DamageChangedEvent>(OnCocoonContainerDamage);
        SubscribeLocalEvent<CocoonContainerComponent, DestructionEventArgs>(OnCocoonContainerDestroyed);
        SubscribeLocalEvent<CocoonContainerComponent, GetVerbsEvent<InteractionVerb>>(OnGetUnwrapVerb);
        SubscribeLocalEvent<CocoonContainerComponent, UnwrapDoAfterEvent>(OnUnwrapDoAfter);

        SubscribeLocalEvent<ContainerManagerComponent, ComponentShutdown>(OnCocoonContainerManagerShutdown);

        SubscribeLocalEvent<CocoonedComponent, RemoveCocoonAlertEvent>(OnRemoveCocoonAlert);
        SubscribeLocalEvent<CocoonedComponent, AttackAttemptEvent>(OnCocoonedAttackAttempt);

        SubscribeLocalEvent<CocoonedComponent, BeingGibbedEvent>(OnCocoonedVictimGibbed);
    }

    private void OnCocoonContainerDamage(EntityUid uid, CocoonContainerComponent component, DamageChangedEvent args)
    {
        // Skip if we're already processing damage for this entity (prevents recursion from SetDamage)
        if (component.ProcessingDamage)
            return;

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

        // Mark as processing to prevent recursion
        component.ProcessingDamage = true;

        try
        {
            // Set the damage to only the absorbed portion
            _damageable.SetDamage(uid, args.Damageable, newDamage);
        }
        finally
        {
            component.ProcessingDamage = false;
        }

        // Pass the reduced damage to the victim inside
        if (component.Victim != null && Exists(component.Victim.Value))
        {
            // Apply the reduced damage directly to the victim
            _damageable.TryChangeDamage(component.Victim.Value, victimDamage, origin: args.Origin);
        }

        // The DestructibleComponent will automatically trigger when damage reaches the threshold
    }

    private void OnCocoonContainerDestroyed(EntityUid uid, CocoonContainerComponent component, DestructionEventArgs args)
    {
        PlayCocoonRemovalSound(uid);

        // Show popup to victim - OnCocoonContainerShutdown will handle cleanup
        if (component.Victim != null && Exists(component.Victim.Value))
        {
            _popups.PopupEntity(Loc.GetString("arachnid-cocoon-broken"), component.Victim.Value, component.Victim.Value, PopupType.LargeCaution);
        }
    }

    /// <summary>
    ///     Plays a sound at the entity's location for everyone within range.
    /// </summary>
    private void PlaySoundAtEntity(EntityUid uid, SoundPathSpecifier sound, float range = 10f)
    {
        var mapCoords = _transform.GetMapCoordinates(uid);
        var filter = Filter.Empty().AddInRange(mapCoords, range);
        var entityCoords = _transform.ToCoordinates(mapCoords);
        _audio.PlayStatic(sound, filter, entityCoords, true);
    }

    /// <summary>
    ///     Plays the cocoon removal sound for everyone within range.
    /// </summary>
    private void PlayCocoonRemovalSound(EntityUid uid)
    {
        PlaySoundAtEntity(uid, new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_breakout.ogg"));
    }

    /// <summary>
    /// Applies effects to a victim when they are cocooned.
    /// </summary>
    private void SetupVictimEffects(EntityUid victim)
    {
        // Force prone
        if (HasComp<StandingStateComponent>(victim))
        {
            _standing.Down(victim);
        }

        EnsureComp<BlockMovementComponent>(victim);

        EnsureComp<MumbleAccentComponent>(victim);
        EnsureComp<TemporaryBlindnessComponent>(victim);
    }

    private void OnCocoonContainerShutdown(EntityUid uid, CocoonContainerComponent component, ComponentShutdown args)
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
            RemComp<BlockMovementComponent>(victim);

        if (HasComp<MumbleAccentComponent>(victim))
        {
            RemComp<MumbleAccentComponent>(victim);
        }

        if (HasComp<TemporaryBlindnessComponent>(victim))
        {
            RemComp<TemporaryBlindnessComponent>(victim);
        }
    }

    /// <summary>
    /// Empties the cocoon container, removing all entities to prevent them from being deleted.
    /// </summary>
    private void EmptyCocoonContainer(EntityUid uid)
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

    /// <summary>
    /// Ensures the cocoon container is empty before ContainerManagerComponent shutdown.
    /// This prevents InternalShutdown from deleting contained entities (like gibbed body parts).
    /// EmptyAllContainersBehaviour should handle this, but we ensure it here as a safety measure.
    /// </summary>
    private void OnCocoonContainerManagerShutdown(EntityUid uid, ContainerManagerComponent component, ComponentShutdown args)
    {
        // Only handle cocoon containers
        if (!HasComp<CocoonContainerComponent>(uid))
            return;

        EmptyCocoonContainer(uid);
    }

    private void OnWrapAction(EntityUid uid, CocoonerComponent component, ref WrapActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        var target = args.Target;

        if (target == user)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            _popups.PopupEntity(Loc.GetString("arachnid-wrap-invalid-target"), user, user);
            return;
        }

        // Check if target is already in a cocoon container
        if (_container.TryGetContainingContainer(target, out var existingContainer) &&
            HasComp<CocoonContainerComponent>(existingContainer.Owner))
        {
            _popups.PopupEntity(Loc.GetString("arachnid-wrap-already"), user, user);
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
                _popups.PopupEntity(Loc.GetString("arachnid-wrap-failure-hunger"), user, user);
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

        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(user):player} is trying to cocoon {ToPrettyString(target):player}");

        PlaySoundAtEntity(target, new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_start.ogg"));

        _popups.PopupEntity(Loc.GetString("arachnid-wrap-start-user", ("target", target)), user, user);
        _popups.PopupEntity(Loc.GetString("arachnid-wrap-start-target", ("user", user)), target, target, PopupType.LargeCaution);

        args.Handled = true;
    }

    private void OnWrapDoAfter(EntityUid uid, CocoonerComponent component, ref WrapDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var performer = args.User;
        var target = args.Args.Target.Value;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        // Check if target is already in a cocoon container
        if (_container.TryGetContainingContainer(target, out var existingContainer) &&
            HasComp<CocoonContainerComponent>(existingContainer.Owner))
        {
            _popups.PopupEntity(Loc.GetString("arachnid-wrap-already"), performer, performer);
            return;
        }

        if (!_blocker.CanInteract(performer, target))
            return;

        // Only consume hunger if the entity has a HungerComponent
        if (TryComp<Content.Shared.Nutrition.Components.HungerComponent>(performer, out var hunger))
        {
            _hunger.ModifyHunger(performer, -component.HungerCost);
        }

        // Spawn cocoon container at target's position
        var targetCoords = _transform.GetMapCoordinates(target);
        var cocoonContainer = Spawn("CocoonContainer", targetCoords);

        // Set up the container component
        if (!TryComp<CocoonContainerComponent>(cocoonContainer, out var cocoonComp))
        {
            Log.Error("CocoonContainer spawned without CocoonContainerComponent!");
            Del(cocoonContainer);
            return;
        }

        cocoonComp.Victim = target;

        Dirty(cocoonContainer, cocoonComp);

        // Drop all items from victim's hands before inserting
        if (TryComp<HandsComponent>(target, out var hands))
        {
            foreach (var hand in _hands.EnumerateHands(target, hands))
            {
                if (hand.HeldEntity != null)
                {
                    _hands.TryDrop(target, hand, checkActionBlocker: false);
                }
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
        SetupVictimEffects(target);

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

        PlaySoundAtEntity(cocoonContainer, new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_end.ogg"));

        _popups.PopupEntity(Loc.GetString("arachnid-wrap-complete-user", ("target", target)), performer, performer);
        _popups.PopupEntity(Loc.GetString("arachnid-wrap-complete-target"), target, target, PopupType.LargeCaution);

        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} has cocooned {ToPrettyString(target):player}");

        args.Handled = true;
    }

    private void OnUnwrapDoAfter(EntityUid uid, CocoonContainerComponent component, ref UnwrapDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        // Play cocoon removal sound before deletion (entity must be valid for coordinates)
        PlayCocoonRemovalSound(uid);

        // Empty the container before deletion
        // OnCocoonContainerManagerShutdown handles this for destructible-triggered destruction,
        // but we need to do it manually here since Del() queues deletion and shutdowns happen later
        EmptyCocoonContainer(uid);

        // Delete the container - OnCocoonContainerShutdown will handle victim cleanup
        Del(uid);

        if (component.Victim != null && Exists(component.Victim.Value))
        {
            var victim = component.Victim.Value;
            _popups.PopupEntity(Loc.GetString("arachnid-unwrap-user", ("target", victim)), args.User, args.User);
            _popups.PopupEntity(Loc.GetString("arachnid-unwrap-target", ("user", args.User)), victim, victim);
        }
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
                    TimeSpan.FromSeconds(10.0f),
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

                PlaySoundAtEntity(uid, new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_start.ogg"));

                if (component.Victim != null && Exists(component.Victim.Value))
                {
                    var victim = component.Victim.Value;
                    _popups.PopupEntity(Loc.GetString("arachnid-unwrap-start-user", ("target", victim)), args.User, args.User);
                    _popups.PopupEntity(Loc.GetString("arachnid-unwrap-start-target", ("user", args.User)), victim, victim);
                }
                else
                {
                    _popups.PopupEntity(Loc.GetString("arachnid-unwrap-start-user-empty"), args.User, args.User);
                }
            }
        };
        args.Verbs.Add(unwrapVerb);
    }

    /// <summary>
    /// Breaks the cocoon container and releases the victim.
    /// </summary>
    public void BreakCocoon(Entity<CocoonContainerComponent> cocoon)
    {
        // Show popup to victim
        if (cocoon.Comp.Victim != null && Exists(cocoon.Comp.Victim.Value))
        {
            _popups.PopupEntity(Loc.GetString("arachnid-cocoon-broken"), cocoon.Comp.Victim.Value, cocoon.Comp.Victim.Value, PopupType.LargeCaution);
        }

        // Play cocoon removal sound before deletion (entity must be valid for coordinates)
        PlayCocoonRemovalSound(cocoon);

        // Empty the container before deletion
        // OnCocoonContainerManagerShutdown handles this for destructible-triggered destruction,
        // but we need to do it manually here since Del() queues deletion and shutdowns happen later
        EmptyCocoonContainer(cocoon);

        // Delete the container - OnCocoonContainerShutdown will handle victim cleanup
        Del(cocoon);
    }

    private void OnRemoveCocoonAlert(Entity<CocoonedComponent> ent, ref RemoveCocoonAlertEvent args)
    {
        if (args.Handled)
            return;

        // Show the popup message that they can't free themselves
        _popups.PopupEntity(Loc.GetString("arachnid-unwrap-self"), ent.Owner, ent.Owner);
        args.Handled = true;
    }

    private void OnCocoonedAttackAttempt(Entity<CocoonedComponent> ent, ref AttackAttemptEvent args)
    {
        // Prevent cocooned victims from attacking at all (similar to handcuffs)
        args.Cancel();
    }

    private void OnCocoonedVictimGibbed(Entity<CocoonedComponent> ent, ref BeingGibbedEvent args)
    {
        // Find the cocoon container that contains this victim
        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        if (!TryComp<CocoonContainerComponent>(container.Owner, out var cocoonComp))
            return;

        // Move all gibbed parts into the cocoon container
        if (!_container.TryGetContainer(container.Owner, CocoonContainerId, out var victimContainer))
            return;

        foreach (var gibbedPart in args.GibbedParts)
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
