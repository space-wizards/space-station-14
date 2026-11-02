using Content.Shared.ActionBlocker;
using Content.Shared.CF.Factions;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusIcon;
using Content.Shared.Weapons.Melee;
using System.Numerics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Off-guard while a creature is flanked. Reach is a sensor fixture. The line test runs only for bodies
/// that sensor is touching, and only when a contact or a move says the geometry may have changed.
/// </summary>
public sealed partial class CFFlankingSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private NpcFactionSystem _factions = default!;
    [Dependency] private CFFactionSystem _cfFactions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private CFDyingSystem _dying = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedStatusIconSystem _statusIcons = default!;

    /// <summary>Foe to the allies whose reach sensors currently overlap that foe.</summary>
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _inReach = new();

    /// <summary>Ally to the foes their sensor currently overlaps.</summary>
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _overlapping = new();

    private readonly HashSet<EntityUid> _dirty = new();
    private readonly List<EntityUid> _dirtyCopy = new();
    private readonly List<EntityUid> _pair = new();
    private readonly HashSet<EntityUid> _flankers = new();

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    public override void Update(float frameTime)
    {
        if (!_net.IsServer || _dirty.Count == 0)
            return;

        _dirtyCopy.Clear();
        foreach (var uid in _dirty)
            _dirtyCopy.Add(uid);

        _dirty.Clear();
        foreach (var uid in _dirtyCopy)
        {
            if (!TerminatingOrDeleted(uid))
                Evaluate(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnMobInit(Entity<MobStateComponent> ent, ref MapInitEvent args)
    {
        EnsureSensor(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnFixturesInit(Entity<FixturesComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<MobStateComponent>(ent))
            return;

        EnsureSensor(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnSensorShutdown(Entity<CFFlankSensorComponent> ent, ref ComponentShutdown args)
    {
        if (!_net.IsServer)
            return;

        if (MetaData(ent.Owner).EntityLifeStage < EntityLifeStage.Terminating
            && _fixtures.GetFixtureOrNull(ent.Owner, CFFlankSensorComponent.FixtureId) != null)
            _fixtures.DestroyFixture(ent.Owner, CFFlankSensorComponent.FixtureId);

        if (!_overlapping.TryGetValue(ent.Owner, out var targets))
            return;

        _pair.Clear();
        foreach (var target in targets)
            _pair.Add(target);

        _overlapping.Remove(ent.Owner);
        foreach (var target in _pair)
            RemoveOverlap(ent.Owner, target);
    }

    [SubscribeLocalEvent]
    private void OnPreventCollide(Entity<CFFlankSensorComponent> ent, ref PreventCollideEvent args)
    {
        if (!IsSensor(ent.Owner, args.OurFixture))
            return;

        // The real body still collides. Only the reach circle is filtered.
        if (args.OtherEntity == ent.Owner
            || !HasComp<MobStateComponent>(args.OtherEntity)
            || !IsHostileTo(ent.Owner, args.OtherEntity))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnStartCollide(Entity<CFFlankSensorComponent> ent, ref StartCollideEvent args)
    {
        if (!_net.IsServer || args.OurFixtureId != CFFlankSensorComponent.FixtureId)
            return;

        if (args.OtherEntity == ent.Owner || !HasComp<MobStateComponent>(args.OtherEntity))
            return;

        AddOverlap(ent.Owner, args.OtherEntity);
    }

    [SubscribeLocalEvent]
    private void OnEndCollide(Entity<CFFlankSensorComponent> ent, ref EndCollideEvent args)
    {
        if (!_net.IsServer || args.OurFixtureId != CFFlankSensorComponent.FixtureId)
            return;

        RemoveOverlap(ent.Owner, args.OtherEntity);
    }

    [SubscribeLocalEvent]
    private void OnMove(Entity<MobStateComponent> ent, ref MoveEvent args)
    {
        if (!_net.IsServer || args.OnlyRotation)
            return;

        if (IsDeadOrDying(ent.Owner, ent.Comp))
            return;

        // The circle already tracks enter and leave. A move only matters while a contact exists,
        // because that is when the line between centers can slide off the foe.
        if (_overlapping.TryGetValue(ent.Owner, out var targets))
        {
            foreach (var target in targets)
                _dirty.Add(target);
        }

        if (_inReach.ContainsKey(ent.Owner))
            _dirty.Add(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnRevived(Entity<CFCombatantComponent> ent, ref CFCombatantRevivedEvent args)
    {
        if (!_net.IsServer)
            return;

        MarkDirty(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnMobState(MobStateChangedEvent args)
    {
        if (!_net.IsServer)
            return;

        MarkDirty(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnEquip(Entity<MobStateComponent> ent, ref DidEquipHandEvent args)
    {
        if (!_net.IsServer)
            return;

        EnsureSensor(ent.Owner);
        MarkDirty(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnUnequip(Entity<MobStateComponent> ent, ref DidUnequipHandEvent args)
    {
        if (!_net.IsServer)
            return;

        EnsureSensor(ent.Owner);
        MarkDirty(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnReachWeaponInit(Entity<CFMeleeWeaponComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.ReachFeet <= CFFlanking.DefaultReachFeet)
            return;

        if (!TryComp(ent.Owner, out MeleeWeaponComponent? melee))
            return;

        var range = CFFlanking.Ss14SwingRange(ent.Comp.ReachFeet);
        if (melee.Range >= range)
            return;

        melee.Range = range;
        Dirty(ent.Owner, melee);
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<CFOffGuardComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Active)
            return;

        args.PushMarkup(Loc.GetString("cf-combat-off-guard-examine"));
    }

    /// <summary>
    /// True when <paramref name="attacker"/> is flanking <paramref name="target"/> with an ally.
    /// </summary>
    public bool IsOffGuardTo(EntityUid target, EntityUid attacker)
    {
        if (!_net.IsServer)
            return false;

        Evaluate(target);
        return TryComp(target, out CFOffGuardComponent? comp) && comp.FlankedBy.Contains(attacker);
    }

    private void EnsureSensor(EntityUid uid)
    {
        if (!_net.IsServer || TerminatingOrDeleted(uid) || !HasComp<FixturesComponent>(uid))
            return;

        var reach = GetMeleeReachFeet(uid);
        var radius = MathF.Min(CFFlanking.SearchRange, CFFlanking.ReachInUnits(reach));
        if (radius <= 0f)
        {
            if (HasComp<CFFlankSensorComponent>(uid))
                RemComp<CFFlankSensorComponent>(uid);
            return;
        }

        var sensor = EnsureComp<CFFlankSensorComponent>(uid);
        var existing = _fixtures.GetFixtureOrNull(uid, CFFlankSensorComponent.FixtureId);
        if (sensor.ReachFeet == reach && existing != null)
            return;

        if (existing != null)
            _fixtures.DestroyFixture(uid, CFFlankSensorComponent.FixtureId);

        var created = _fixtures.TryCreateFixture(
            uid,
            new PhysShapeCircle(radius),
            CFFlankSensorComponent.FixtureId,
            density: 0f,
            hard: false,
            collisionLayer: (int) CollisionGroup.None,
            collisionMask: (int) CollisionGroup.MobLayer);

        if (created)
            sensor.ReachFeet = reach;
    }

    private bool IsSensor(EntityUid uid, Fixture fixture)
    {
        return ReferenceEquals(_fixtures.GetFixtureOrNull(uid, CFFlankSensorComponent.FixtureId), fixture);
    }

    private void AddOverlap(EntityUid flanker, EntityUid target)
    {
        if (flanker == target)
            return;

        if (!_inReach.TryGetValue(target, out var flankers))
        {
            flankers = new HashSet<EntityUid>();
            _inReach[target] = flankers;
        }

        if (!flankers.Add(flanker))
            return;

        if (!_overlapping.TryGetValue(flanker, out var targets))
        {
            targets = new HashSet<EntityUid>();
            _overlapping[flanker] = targets;
        }

        targets.Add(target);
        _dirty.Add(target);
    }

    private void RemoveOverlap(EntityUid flanker, EntityUid target)
    {
        if (_inReach.TryGetValue(target, out var flankers))
        {
            flankers.Remove(flanker);
            if (flankers.Count == 0)
                _inReach.Remove(target);
        }

        if (_overlapping.TryGetValue(flanker, out var targets))
        {
            targets.Remove(target);
            if (targets.Count == 0)
                _overlapping.Remove(flanker);
        }

        _dirty.Add(target);
    }

    private void MarkDirty(EntityUid uid)
    {
        _dirty.Add(uid);
        if (!_overlapping.TryGetValue(uid, out var targets))
            return;

        foreach (var target in targets)
            _dirty.Add(target);
    }

    private void Evaluate(EntityUid target)
    {
        if (!TryComp(target, out MobStateComponent? state) || IsDeadOrDying(target, state))
        {
            Clear(target, announce: false);
            return;
        }

        if (!TryGetWorld(target, out var targetPos))
        {
            Clear(target, announce: false);
            return;
        }

        _pair.Clear();
        if (_inReach.TryGetValue(target, out var overlapping))
        {
            foreach (var uid in overlapping)
                _pair.Add(uid);
        }

        _flankers.Clear();
        for (var i = 0; i < _pair.Count; i++)
        {
            var uid = _pair[i];
            if (!TryFlanker(uid, target, out var position))
                continue;

            for (var j = i + 1; j < _pair.Count; j++)
            {
                var other = _pair[j];
                if (!TryFlanker(other, target, out var otherPos))
                    continue;

                if (!PairFlanks(uid, other, target))
                    continue;

                if (!CFFlanking.LineFlanks(position, otherPos, targetPos.Position))
                    continue;

                _flankers.Add(uid);
                _flankers.Add(other);
            }
        }

        if (_flankers.Count == 0)
        {
            Clear(target);
            return;
        }

        if (TryComp(target, out CFOffGuardComponent? existing)
            && existing.Active
            && existing.FlankedBy.SetEquals(_flankers))
            return;

        var comp = existing ?? EnsureComp<CFOffGuardComponent>(target);
        var wasActive = comp.Active;
        comp.FlankedBy.Clear();
        foreach (var flanker in _flankers)
            comp.FlankedBy.Add(flanker);

        comp.Active = true;
        EnsureFlankIcon(target);
        if (wasActive)
            return;

        Dirty(target, comp);
        _popup.PopupEntity(Loc.GetString("cf-combat-off-guard"), target, PopupType.Medium);
    }

    private bool TryFlanker(EntityUid uid, EntityUid target, out Vector2 position)
    {
        position = default;
        if (uid == target || TerminatingOrDeleted(uid) || !CanAct(uid))
            return false;

        if (!TryGetWorld(uid, out var coords) || !TryGetWorld(target, out var targetPos) || coords.MapId != targetPos.MapId)
            return false;

        position = coords.Position;
        return true;
    }

    private void Clear(EntityUid target, bool announce = true)
    {
        var wasActive = false;
        if (TryComp(target, out CFOffGuardComponent? comp))
        {
            wasActive = comp.Active;
            RemComp<CFOffGuardComponent>(target);
        }

        RemoveFlankIcon(target);
        if (wasActive && announce)
            _popup.PopupEntity(Loc.GetString("cf-combat-off-guard-cleared"), target, PopupType.Small);
    }

    private bool IsDeadOrDying(EntityUid uid, MobStateComponent state)
    {
        if (state.CurrentState != MobState.Alive)
            return true;

        return TryComp(uid, out CFCombatantComponent? combat)
            && (_dying.IsDying(combat) || _dying.IsDown(combat));
    }

    private bool CanAct(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) || _container.IsEntityInContainer(uid))
            return false;

        if (TryComp(uid, out MobStateComponent? state) && state.CurrentState != MobState.Alive)
            return false;

        if (TryComp(uid, out CFCombatantComponent? combat) && _dying.IsDown(combat))
            return false;

        return _blocker.CanAttack(uid);
    }

    private int GetMeleeReachFeet(EntityUid uid)
    {
        var best = 0;
        if (_hands.GetHandCount(uid) > 0)
        {
            foreach (var held in _hands.EnumerateHeld(uid))
            {
                if (TryComp(held, out CFMeleeWeaponComponent? weapon))
                    best = Math.Max(best, weapon.ReachFeet);
                else if (HasComp<MeleeWeaponComponent>(held) && !HasComp<CFRangedWeaponComponent>(held))
                    best = Math.Max(best, CFFlanking.DefaultReachFeet);
            }

            if (_hands.CountFreeHands(uid) > 0)
                best = Math.Max(best, CFFlanking.DefaultReachFeet);
        }
        else if (TryComp(uid, out CFMeleeWeaponComponent? body))
        {
            best = Math.Max(best, body.ReachFeet);
        }
        else if (HasComp<MeleeWeaponComponent>(uid))
        {
            best = CFFlanking.DefaultReachFeet;
        }

        return best;
    }

    /// <summary>
    /// The two flankers share a side, and each one is an enemy of the target.
    /// </summary>
    private bool PairFlanks(EntityUid a, EntityUid b, EntityUid target)
    {
        return AreAllies(a, b) && IsHostileTo(a, target) && IsHostileTo(b, target);
    }

    /// <summary>
    /// Party members flank together. Creatures with no CF faction fall back to the stock NPC faction,
    /// or two player characters with no NPC faction.
    /// </summary>
    private bool AreAllies(EntityUid a, EntityUid b)
    {
        if (a == b)
            return false;

        if (HasComp<CFFactionComponent>(a) && HasComp<CFFactionComponent>(b))
            return _cfFactions.GetAlliance(a, b) == CFAlliance.Party;

        if (_factions.IsEntityFriendly(a, b) || _factions.IsEntityFriendly(b, a))
            return true;

        var aFaction = _factions.IsEntityFriendly(a, a);
        var bFaction = _factions.IsEntityFriendly(b, b);
        return !aFaction && !bFaction && HasComp<ActorComponent>(a) && HasComp<ActorComponent>(b);
    }

    /// <summary>
    /// CF factions must be Opposition. Anyone else is hostile when they are not allies,
    /// so two players can still flank a creature that has no CF faction.
    /// </summary>
    private bool IsHostileTo(EntityUid flanker, EntityUid target)
    {
        if (flanker == target)
            return false;

        if (HasComp<CFFactionComponent>(flanker) && HasComp<CFFactionComponent>(target))
            return _cfFactions.GetAlliance(flanker, target) == CFAlliance.Opposition;

        return !AreAllies(flanker, target);
    }

    private void EnsureFlankIcon(EntityUid uid)
    {
        if (_statusIcons.CFTryAddStatusIcon(uid))
            EnsureComp<CFAddedStatusIconComponent>(uid);
    }

    private void RemoveFlankIcon(EntityUid uid)
    {
        if (!HasComp<CFAddedStatusIconComponent>(uid))
            return;

        RemComp<CFAddedStatusIconComponent>(uid);
        _statusIcons.CFRemoveStatusIcon(uid);
    }

    private bool TryGetWorld(EntityUid uid, out MapCoordinates coords)
    {
        coords = default;
        if (!TryComp(uid, out TransformComponent? xform))
            return false;

        coords = _transform.GetMapCoordinates(uid, xform);
        return coords.MapId != MapId.Nullspace;
    }
}
