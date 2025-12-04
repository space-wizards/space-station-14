// SPDX-FileCopyrightText: 2025 Drywink <43855731+Drywink@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
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
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Rotation;
using Content.Shared.Species.Arachnid;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Systems;
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
        SubscribeLocalEvent<CocoonContainerComponent, DamageModifyEvent>(OnCocoonContainerDamage);
        SubscribeLocalEvent<CocoonContainerComponent, GetVerbsEvent<InteractionVerb>>(OnGetUnwrapVerb);
        SubscribeLocalEvent<CocoonContainerComponent, UnwrapDoAfterEvent>(OnUnwrapDoAfter);

        SubscribeLocalEvent<CocoonedComponent, RemoveCocoonAlertEvent>(OnRemoveCocoonAlert);
    }

    private void OnCocoonContainerDamage(Entity<CocoonContainerComponent> ent, ref DamageModifyEvent args)
    {
        // Only absorb positive damage
        if (!args.OriginalDamage.AnyPositive())
            return;

        var originalTotalDamage = args.OriginalDamage.GetTotal().Float();
        if (originalTotalDamage <= 0)
            return;

        // Calculate percentage of the original damage to absorb
        var absorbedDamage = originalTotalDamage * ent.Comp.AbsorbPercentage;

        // Reduce the damage by the absorb percentage (victim only takes the remainder)
        // Apply coefficient to all damage types that were originally present
        var reducePercentage = 1f - ent.Comp.AbsorbPercentage;
        var modifier = new DamageModifierSet();
        foreach (var key in args.OriginalDamage.DamageDict.Keys)
        {
            modifier.Coefficients.TryAdd(key, reducePercentage);
        }
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);

        // Accumulate the absorbed damage on the cocoon container
        ent.Comp.AccumulatedDamage += absorbedDamage;
        Dirty(ent, ent.Comp);

        // Pass the reduced damage to the victim inside
        if (ent.Comp.Victim != null && Exists(ent.Comp.Victim.Value))
        {
            // Apply the reduced damage directly to the victim
            _damageable.TryChangeDamage(ent.Comp.Victim.Value, args.Damage, origin: args.Origin);
        }

        // The container itself takes minimal/no damage (we handle breaking via accumulated damage)
        // Set damage to zero so the container doesn't take structural damage
        args.Damage = new DamageSpecifier();

        // Break the cocoon if it reaches max damage
        if (ent.Comp.AccumulatedDamage >= ent.Comp.MaxDamage)
        {
            BreakCocoon(ent);
        }
    }

    /// <summary>
    ///     Plays the cocoon removal sound for everyone within range.
    /// </summary>
    private void PlayCocoonRemovalSound(EntityUid uid)
    {
        var mapCoords = _transform.GetMapCoordinates(uid);
        var filter = Filter.Empty().AddInRange(mapCoords, 10f);
        var entityCoords = _transform.ToCoordinates(mapCoords);
        _audio.PlayStatic(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_breakout.ogg"), filter, entityCoords, true);
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

        if (!HasComp<BlockMovementComponent>(victim))
        {
            AddComp<BlockMovementComponent>(victim);
        }

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

        var mapCoords = _transform.GetMapCoordinates(target);
        var filter = Filter.Empty().AddInRange(mapCoords, 10f);
        var entityCoords = _transform.ToCoordinates(mapCoords);
        _audio.PlayStatic(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_start.ogg"), filter, entityCoords, true);

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

        var filter = Filter.Empty().AddInRange(targetCoords, 10f);
        var entityCoords = _transform.ToCoordinates(targetCoords);
        _audio.PlayStatic(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_end.ogg"), filter, entityCoords, true);

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

        // Play cocoon removal sound for everyone within 10 meters
        PlayCocoonRemovalSound(uid);

        // Remove virtual items from victim's hands before removing from container
        if (component.Victim != null && Exists(component.Victim.Value))
        {
            var victim = component.Victim.Value;
            _virtualItem.DeleteInHandsMatching(victim, uid);

            // Remove CocoonedComponent and clear alert
            if (TryComp<CocoonedComponent>(victim, out var cocoonedComp))
            {
                _alerts.ClearAlert(victim, cocoonedComp.CocoonedAlert);
                RemComp<CocoonedComponent>(victim);
            }

            if (_container.TryGetContainer(uid, CocoonContainerId, out var container))
            {
                _container.Remove(victim, container);
            }
        }

        // Delete the container
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

                var mapCoords = _transform.GetMapCoordinates(uid);
                var filter = Filter.Empty().AddInRange(mapCoords, 10f);
                var entityCoords = _transform.ToCoordinates(mapCoords);
                _audio.PlayStatic(new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_start.ogg"), filter, entityCoords, true);

                var targetName = component.Victim != null && Exists(component.Victim.Value) ? component.Victim.Value : uid;
                _popups.PopupEntity(Loc.GetString("arachnid-unwrap-start-user", ("target", targetName)), args.User, args.User);
                if (component.Victim != null && Exists(component.Victim.Value))
                {
                    _popups.PopupEntity(Loc.GetString("arachnid-unwrap-start-target", ("user", args.User)), component.Victim.Value, component.Victim.Value);
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
        PlayCocoonRemovalSound(cocoon);

        // Remove victim from container before deleting
        if (cocoon.Comp.Victim != null && Exists(cocoon.Comp.Victim.Value))
        {
            var victim = cocoon.Comp.Victim.Value;
            // Remove virtual items from victim's hands
            _virtualItem.DeleteInHandsMatching(victim, cocoon);

            // Remove CocoonedComponent and clear alert
            if (TryComp<CocoonedComponent>(victim, out var cocoonedComp))
            {
                _alerts.ClearAlert(victim, cocoonedComp.CocoonedAlert);
                RemComp<CocoonedComponent>(victim);
            }

            if (_container.TryGetContainer(cocoon, CocoonContainerId, out var container))
            {
                _container.Remove(victim, container);
            }
            _popups.PopupEntity(Loc.GetString("arachnid-cocoon-broken"), victim, victim, PopupType.LargeCaution);
        }

        // Delete the container
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

}
