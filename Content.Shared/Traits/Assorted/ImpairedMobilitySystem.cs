using System.Linq;
using Content.Shared.Movement.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Wieldable.Components;
using Content.Shared.Popups;
using Content.Shared.Movement.Events;
using Content.Shared.Stunnable;
using Content.Shared.Examine;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Network;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Handles <see cref="ImpairedMobilityComponent"/>
/// </summary>
public sealed class ImpairedMobilitySystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ImpairedMobilityComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ImpairedMobilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ImpairedMobilityComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<ImpairedMobilityComponent, GetStandUpTimeEvent>(OnGetStandUpTime);
        SubscribeLocalEvent<ImpairedMobilityComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<MobilityAidComponent, ExaminedEvent>(OnMobilityAidExamined);
    }

    private void OnInit(Entity<ImpairedMobilityComponent> ent, ref ComponentInit args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(ent);
    }

    private void OnShutdown(Entity<ImpairedMobilityComponent> ent, ref ComponentShutdown args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(ent);
    }

    // Handles movement speed for entities with impaired mobility.
    // Applies a speed penalty, but counteracts it if the entity is holding a non-wielded mobility aid.
    private void OnRefreshMovementSpeed(Entity<ImpairedMobilityComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var (effectiveness, _, _) = GetBestMobilityAid(ent.Owner);
        if (effectiveness <= 0f)
        {
            args.ModifySpeed(ent.Comp.SpeedModifier);
            return;
        }


        // If effectiveness is 1, fully counteract the penalty. If less, partially counteract.
        var normalSpeed = 1.0f;
        var penalty = ent.Comp.SpeedModifier;
        var finalSpeed = penalty + (normalSpeed - penalty) * effectiveness;
        args.ModifySpeed(finalSpeed);
    }

    // Returns the best mobility aid held according to the following logic:
    // 1. Prefer any non-makeshift (proper) aid with highest effectiveness.
    // 2. If only makeshift aids are held, prefer safe (no damage) aids with highest effectiveness.
    // 3. If only dangerous makeshift aids are held, pick the one with highest effectiveness and least damage.
    private (float, MobilityAidComponent?, EntityUid?) GetBestMobilityAid(Entity<HandsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return (0f, null, null);

        var aids = new List<(MobilityAidComponent aid, EntityUid ent, int priority, float effectiveness, float damage)>();

        foreach (var held in _hands.EnumerateHeld(entity))
        {
            if (!TryComp<MobilityAidComponent>(held, out var aid))
                continue;
            if (TryComp<WieldableComponent>(held, out var wieldable) && wieldable.Wielded)
                continue;

            int priority;
            float damage = aid.TripDamage?.GetTotal().Float() ?? 0f;
            if (!aid.IsMakeshift)
                priority = 2; // Proper
            else if (aid.TripDamage == null)
                priority = 1; // Safe
            else
                priority = 0; // Dangerous

            aids.Add((aid, held, priority, aid.Effectiveness, damage));
        }

        if (aids.Count == 0)
            return (0f, null, null);

        // Split dangerous aids from proper/safe aids
        var dangerousAids = aids.Where(x => x.priority == 0).ToList();
        var safeOrProperAids = aids.Where(x => x.priority > 0).ToList();

        if (safeOrProperAids.Count > 0)
        {
            // Prefer highest priority (proper > safe), then highest effectiveness
            var best = safeOrProperAids
                .OrderByDescending(x => x.priority)
                .ThenByDescending(x => x.effectiveness)
                .First();
            return (best.effectiveness, best.aid, best.ent);
        }
        else if (dangerousAids.Count > 0)
        {
            // If dangerous prefer least damage, then highest effectiveness
            var best = dangerousAids
                .OrderBy(x => x.damage)
                .ThenByDescending(x => x.effectiveness)
                .First();
            return (best.effectiveness, best.aid, best.ent);
        }

        return (0f, null, null);
    }

    // Increases the time it takes for entities to stand up from being knocked down. This is intentionally NOT affected by mobility aids.
    private void OnGetStandUpTime(Entity<ImpairedMobilityComponent> ent, ref GetStandUpTimeEvent args)
    {
        args.DoAfterTime *= ent.Comp.StandUpTimeModifier;
    }

    // Handles tripping when moving with makeshift mobility aids
    private void OnMoveInput(Entity<ImpairedMobilityComponent> ent, ref MoveInputEvent args)
    {
        // Only check for tripping if the entity is actually trying to move
        if (!args.HasDirectionalMovement)
            return;

        // Get the best mobility aid being used
        var (_, makeshiftAid, makeshiftAidEntity) = GetBestMobilityAid(ent.Owner);

        // Only trip if using a makeshift mobility aid
        if (makeshiftAid == null || !makeshiftAid.IsMakeshift || makeshiftAid.TripChance <= 0f)
            return;

        var currentTime = _timing.CurTime;

        // Initialize roll
        if (!ent.Comp.NextTripRollTime.HasValue)
        {
            // Set roll time using configured roll intervals
            var initialDelay = _random.NextFloat(ent.Comp.MinTripRollInterval, ent.Comp.MaxTripRollInterval);
            ent.Comp.NextTripRollTime = currentTime + TimeSpan.FromSeconds(initialDelay);
            return;
        }

        if (currentTime < ent.Comp.NextTripRollTime.Value)
            return;

        // Roll for trip chance
        if (!_random.Prob(makeshiftAid.TripChance))
        {
            // Failed to trip, roll again after roll interval
            var nextDelay = _random.NextFloat(ent.Comp.MinTripRollInterval, ent.Comp.MaxTripRollInterval);
            ent.Comp.NextTripRollTime = currentTime + TimeSpan.FromSeconds(nextDelay);
            return;
        }

        // Tripped! Set cooldown
        ent.Comp.LastTripTime = currentTime;
        ent.Comp.NextTripRollTime = currentTime + TimeSpan.FromSeconds(ent.Comp.TripCooldownTime);

        // get tripped dork
        if (_netManager.IsServer)
        {
            // Stun and knock down for 2 seconds
            _stunSystem.TryAddStunDuration(ent.Owner, TimeSpan.FromSeconds(2));
            _stunSystem.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(2));

            // Apply damage if the makeshift aid has trip damage
            if (makeshiftAid.TripDamage != null)
            {
                _damageableSystem.TryChangeDamage(ent.Owner, makeshiftAid.TripDamage);
                _audioSystem.PlayPvs("/Audio/Effects/hit_kick.ogg", ent.Owner);
            }

            // Trip popups
            if (makeshiftAidEntity == null)
                return;

            string selfMessage, othersMessage;
            if (makeshiftAid.TripDamage != null)
            {
                // Dangerous
                selfMessage = Loc.GetString("mobility-aid-trait-trip-self-dangerous", ("mobilityAid", makeshiftAidEntity.Value));
                othersMessage = Loc.GetString("mobility-aid-trait-trip-others-dangerous", ("user", ent.Owner), ("mobilityAid", makeshiftAidEntity.Value));

                _popup.PopupEntity(selfMessage, ent.Owner, ent.Owner, PopupType.SmallCaution);
                _popup.PopupEntity(othersMessage, ent.Owner, Filter.PvsExcept(ent.Owner), true, PopupType.SmallCaution);
            }
            else
            {
                // Safe
                selfMessage = Loc.GetString("mobility-aid-trait-trip-self", ("mobilityAid", makeshiftAidEntity.Value));
                othersMessage = Loc.GetString("mobility-aid-trait-trip-others", ("user", ent.Owner), ("mobilityAid", makeshiftAidEntity.Value));

                _popup.PopupEntity(selfMessage, ent.Owner, ent.Owner);
                _popup.PopupEntity(othersMessage, ent.Owner, Filter.PvsExcept(ent.Owner), true);
            }
        }
    }

    private void OnMobilityAidExamined(EntityUid uid, MobilityAidComponent component, ExaminedEvent args)
    {
        // Only show mobility aid examine text to entities with impaired mobility
        if (!HasComp<ImpairedMobilityComponent>(args.Examiner))
            return;

        if (!args.IsInDetailsRange)
            return;

        // Show different messages based on whether it's makeshift or not
        if (component.IsMakeshift)
        {
            // Check if this makeshift aid can cause damage when tripping
            if (component.TripDamage != null)
            {
                // Dangerous
                var dangerousMessage = Loc.GetString("mobility-aid-examine-makeshift-dangerous");
                args.PushMarkup(dangerousMessage);
            }
            else
            {
                // Makeshift
                var makeshiftMessage = Loc.GetString("mobility-aid-examine-makeshift");
                args.PushMarkup(makeshiftMessage);
            }
        }
        else
        {
            // Proper
            var properMessage = Loc.GetString("mobility-aid-examine-proper");
            args.PushMarkup(properMessage);
        }
    }
}
