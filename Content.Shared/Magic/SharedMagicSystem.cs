using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Lock;
using Content.Shared.Magic.Components;
using Content.Shared.Magic.Events;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.Storage;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Spawners;

namespace Content.Shared.Magic;

// TODO: Move BeforeCast & Prerequirements (like Wizard clothes) to action comp
//   Alt idea - make it its own comp and split, like the Charge PR
// TODO: Move speech to actionComp or again, its own ECS
// TODO: Use the MagicComp just for pure backend things like spawning patterns?
/// <summary>
/// Handles learning and using spells (actions)
/// </summary>
public abstract class SharedMagicSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _seriMan = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MagicComponent, BeforeCastSpellEvent>(OnBeforeCastSpell);

        SubscribeLocalEvent<InstantSpawnSpellEvent>(OnInstantSpawn);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<WorldSpawnSpellEvent>(OnWorldSpawn);
        SubscribeLocalEvent<ProjectileSpellEvent>(OnProjectileSpell);
        SubscribeLocalEvent<ChangeComponentsSpellEvent>(OnChangeComponentsSpell);
        SubscribeLocalEvent<SmiteSpellEvent>(OnSmiteSpell);
        SubscribeLocalEvent<KnockSpellEvent>(OnKnockSpell);
        SubscribeLocalEvent<ChargeSpellEvent>(OnChargeSpell);
        SubscribeLocalEvent<RandomGlobalSpawnSpellEvent>(OnRandomGlobalSpawnSpell);
        SubscribeLocalEvent<MindSwapSpellEvent>(OnMindSwapSpell);
        SubscribeLocalEvent<VoidApplauseSpellEvent>(OnVoidApplause);
    }

    private void OnBeforeCastSpell(Entity<MagicComponent> ent, ref BeforeCastSpellEvent args)
    {
        var comp = ent.Comp;
        var hasReqs = true;

        if (comp.RequiresClothes)
        {
            var enumerator = _inventory.GetSlotEnumerator(args.Performer, SlotFlags.OUTERCLOTHING | SlotFlags.HEAD);
            while (enumerator.MoveNext(out var containerSlot))
            {
                if (containerSlot.ContainedEntity is { } item)
                    hasReqs = HasComp<WizardClothesComponent>(item);
                else
                    hasReqs = false;

                if (!hasReqs)
                    break;
            }
        }

        if (comp.RequiresSpeech && HasComp<MutedComponent>(args.Performer))
            hasReqs = false;

        if (hasReqs)
            return;

        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString("spell-requirements-failed"), args.Performer, args.Performer);

        // TODO: Pre-cast do after, either here or in SharedActionsSystem
    }

    private bool PassesSpellPrerequisites(EntityUid spell, EntityUid performer)
    {
        var ev = new BeforeCastSpellEvent(performer);
        RaiseLocalEvent(spell, ref ev);
        return !ev.Cancelled;
    }

    #region Spells
    #region Instant Spawn Spells
    /// <summary>
    /// Handles the instant action (i.e. on the caster) attempting to spawn an entity.
    /// </summary>
    private void OnInstantSpawn(InstantSpawnSpellEvent args)
    {
        if (args.Handled || !PassesSpellPrerequisites(args.Action, args.Performer))
            return;

        var transform = Transform(args.Performer);

        foreach (var position in GetInstantSpawnPositions(transform, args.PosData))
        {
            SpawnSpellHelper(args.Prototype, position, args.Performer, preventCollide: args.PreventCollideWithCaster);
        }

        Speak(args);
        args.Handled = true;
    }

        /// <summary>
    ///     Gets spawn positions listed on <see cref="InstantSpawnSpellEvent"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private List<EntityCoordinates> GetInstantSpawnPositions(TransformComponent casterXform, MagicInstantSpawnData data)
    {
        switch (data)
        {
            case TargetCasterPos:
                return new List<EntityCoordinates>(1) {casterXform.Coordinates};
            case TargetInFrontSingle:
            {
                var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

                if (!TryComp<MapGridComponent>(casterXform.GridUid, out var mapGrid))
                    return new List<EntityCoordinates>();
                if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
                    return new List<EntityCoordinates>();

                var tileIndex = tileReference.Value.GridIndices;
                return new List<EntityCoordinates>(1) { _mapSystem.GridTileToLocal(casterXform.GridUid.Value, mapGrid, tileIndex) };
            }
            case TargetInFront:
            {
                var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

                if (!TryComp<MapGridComponent>(casterXform.GridUid, out var mapGrid))
                    return new List<EntityCoordinates>();

                if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
                    return new List<EntityCoordinates>();

                var tileIndex = tileReference.Value.GridIndices;
                var coords = _mapSystem.GridTileToLocal(casterXform.GridUid.Value, mapGrid, tileIndex);
                EntityCoordinates coordsPlus;
                EntityCoordinates coordsMinus;

                var dir = casterXform.LocalRotation.GetCardinalDir();
                switch (dir)
                {
                    case Direction.North:
                    case Direction.South:
                    {
                        coordsPlus = _mapSystem.GridTileToLocal(casterXform.GridUid.Value, mapGrid, tileIndex + (1, 0));
                        coordsMinus = _mapSystem.GridTileToLocal(casterXform.GridUid.Value, mapGrid, tileIndex + (-1, 0));
                        return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                    }
                    case Direction.East:
                    case Direction.West:
                    {
                        coordsPlus = _mapSystem.GridTileToLocal(casterXform.GridUid.Value, mapGrid, tileIndex + (0, 1));
                        coordsMinus = _mapSystem.GridTileToLocal(casterXform.GridUid.Value, mapGrid, tileIndex + (0, -1));
                        return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                    }
                }

                return new List<EntityCoordinates>();
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    // End Instant Spawn Spells
    #endregion
    #region World Spawn Spells
    /// <summary>
    /// Spawns entities from a list within range of click.
    /// </summary>
    /// <remarks>
    /// It will offset entities after the first entity based on the OffsetVector2.
    /// </remarks>
    /// <param name="args"> The Spawn Spell Event args.</param>
    private void OnWorldSpawn(WorldSpawnSpellEvent args)
    {
        if (args.Handled || !PassesSpellPrerequisites(args.Action, args.Performer))
            return;

        var targetMapCoords = args.Target;

        WorldSpawnSpellHelper(args.Prototypes, targetMapCoords, args.Performer, args.Lifetime, args.Offset);
        Speak(args);
        args.Handled = true;
    }

    /// <summary>
    /// Loops through a supplied list of entity prototypes and spawns them
    /// </summary>
    /// <remarks>
    /// If an offset of 0, 0 is supplied then the entities will all spawn on the same tile.
    /// Any other offset will spawn entities starting from the source Map Coordinates and will increment the supplied
    /// offset
    /// </remarks>
    /// <param name="entityEntries"> The list of Entities to spawn in</param>
    /// <param name="entityCoords"> Map Coordinates where the entities will spawn</param>
    /// <param name="lifetime"> Check to see if the entities should self delete</param>
    /// <param name="offsetVector2"> A Vector2 offset that the entities will spawn in</param>
    private void WorldSpawnSpellHelper(List<EntitySpawnEntry> entityEntries, EntityCoordinates entityCoords, EntityUid performer, float? lifetime, Vector2 offsetVector2)
    {
        var getProtos = EntitySpawnCollection.GetSpawns(entityEntries, _random);

        var offsetCoords = entityCoords;
        foreach (var proto in getProtos)
        {
            SpawnSpellHelper(proto, offsetCoords, performer, lifetime);
            offsetCoords = offsetCoords.Offset(offsetVector2);
        }
    }
    // End World Spawn Spells
    #endregion
    #region Projectile Spells
    private void OnProjectileSpell(ProjectileSpellEvent ev)
    {
        if (ev.Handled || !PassesSpellPrerequisites(ev.Action, ev.Performer) || !_net.IsServer)
            return;

        ev.Handled = true;
        Speak(ev);

        var xform = Transform(ev.Performer);
        var fromCoords = xform.Coordinates;
        var toCoords = ev.Target;
        var userVelocity = _physics.GetMapLinearVelocity(ev.Performer);

        // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
        var fromMap = fromCoords.ToMap(EntityManager, _transform);
        var spawnCoords = _mapManager.TryFindGridAt(fromMap, out var gridUid, out _)
            ? fromCoords.WithEntityId(gridUid, EntityManager)
            : new(_mapManager.GetMapEntityId(fromMap.MapId), fromMap.Position);

        var ent = Spawn(ev.Prototype, spawnCoords);
        var direction = toCoords.ToMapPos(EntityManager, _transform) -
                        spawnCoords.ToMapPos(EntityManager, _transform);
        _gunSystem.ShootProjectile(ent, direction, userVelocity, ev.Performer, ev.Performer);
    }
    // End Projectile Spells
    #endregion
    #region Change Component Spells
    // staves.yml ActionRGB light
    private void OnChangeComponentsSpell(ChangeComponentsSpellEvent ev)
    {
        if (ev.Handled || !PassesSpellPrerequisites(ev.Action, ev.Performer))
            return;

        ev.Handled = true;
        if (ev.DoSpeech)
            Speak(ev);

        RemoveComponents(ev.Target, ev.ToRemove);
        AddComponents(ev.Target, ev.ToAdd);
    }
    // End Change Component Spells
    #endregion
    #region Teleport Spells
    // TODO: Rename to teleport clicked spell?
    /// <summary>
    /// Teleports the user to the clicked location
    /// </summary>
    /// <param name="args"></param>
    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled || !PassesSpellPrerequisites(args.Action, args.Performer))
            return;

        var transform = Transform(args.Performer);
        if (transform.MapID != _transform.GetMapId(args.Target) || !_interaction.InRangeUnobstructed(args.Performer, args.Target, range: 1000F, collisionMask: CollisionGroup.Opaque, popup: true))
            return;

        _transform.SetCoordinates(args.Performer, args.Target);
        _transform.AttachToGridOrMap(args.Performer, transform);
        Speak(args);
        args.Handled = true;
    }

    public virtual void OnVoidApplause(VoidApplauseSpellEvent ev)
    {
        if (ev.Handled || !PassesSpellPrerequisites(ev.Action, ev.Performer))
            return;

        ev.Handled = true;
        Speak(ev);

        _transform.SwapPositions(ev.Performer, ev.Target);
    }
    // End Teleport Spells
    #endregion
    #region Spell Helpers
    private void SpawnSpellHelper(string? proto, EntityCoordinates position, EntityUid performer, float? lifetime = null, bool preventCollide = false)
    {
        if (!_net.IsServer)
            return;

        var ent = Spawn(proto, position.SnapToGrid(EntityManager, _mapManager));

        if (lifetime != null)
        {
            var comp = EnsureComp<TimedDespawnComponent>(ent);
            comp.Lifetime = lifetime.Value;
        }

        if (preventCollide)
        {
            var comp = EnsureComp<PreventCollideComponent>(ent);
            comp.Uid = performer;
        }
    }

    private void AddComponents(EntityUid target, ComponentRegistry comps)
    {
        foreach (var (name, data) in comps)
        {
            if (HasComp(target, data.Component.GetType()))
                continue;

            var component = (Component)_compFact.GetComponent(name);
            var temp = (object)component;
            _seriMan.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(target, (Component)temp!);
        }
    }

    private void RemoveComponents(EntityUid target, HashSet<string> comps)
    {
        foreach (var toRemove in comps)
        {
            if (_compFact.TryGetRegistration(toRemove, out var registration))
                RemComp(target, registration.Type);
        }
    }
    // End Spell Helpers
    #endregion
    #region Touch Spells
    private void OnSmiteSpell(SmiteSpellEvent ev)
    {
        if (ev.Handled || !PassesSpellPrerequisites(ev.Action, ev.Performer))
            return;

        ev.Handled = true;
        Speak(ev);

        var direction = _transform.GetMapCoordinates(ev.Target, Transform(ev.Target)).Position - _transform.GetMapCoordinates(ev.Performer, Transform(ev.Performer)).Position;
        var impulseVector = direction * 10000;

        _physics.ApplyLinearImpulse(ev.Target, impulseVector);

        if (!TryComp<BodyComponent>(ev.Target, out var body))
            return;

        _body.GibBody(ev.Target, true, body);
    }

    // End Touch Spells
    #endregion
    #region Knock Spells
    /// <summary>
    /// Opens all doors and locks within range
    /// </summary>
    /// <param name="args"></param>
    private void OnKnockSpell(KnockSpellEvent args)
    {
        if (args.Handled || !PassesSpellPrerequisites(args.Action, args.Performer))
            return;

        args.Handled = true;
        Speak(args);

        var transform = Transform(args.Performer);

        // Look for doors and lockers, and don't open/unlock them if they're already opened/unlocked.
        foreach (var target in _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(args.Performer, transform), args.Range, flags: LookupFlags.Dynamic | LookupFlags.Static))
        {
            if (!_interaction.InRangeUnobstructed(args.Performer, target, range: 0, collisionMask: CollisionGroup.Opaque))
                continue;

            if (TryComp<DoorBoltComponent>(target, out var doorBoltComp) && doorBoltComp.BoltsDown)
                _door.SetBoltsDown((target, doorBoltComp), false, predicted: true);

            if (TryComp<DoorComponent>(target, out var doorComp) && doorComp.State is not DoorState.Open)
                _door.StartOpening(target);

            if (TryComp<LockComponent>(target, out var lockComp) && lockComp.Locked)
                _lock.Unlock(target, args.Performer, lockComp);
        }
    }
    // End Knock Spells
    #endregion
    #region Charge Spells
    // TODO: Future support to charge other items
    private void OnChargeSpell(ChargeSpellEvent ev)
    {
        if (ev.Handled || !PassesSpellPrerequisites(ev.Action, ev.Performer) || !TryComp<HandsComponent>(ev.Performer, out var handsComp))
            return;

        EntityUid? wand = null;
        foreach (var item in _hands.EnumerateHeld(ev.Performer, handsComp))
        {
            if (!_tag.HasTag(item, ev.WandTag))
                continue;

            wand = item;
        }

        ev.Handled = true;
        Speak(ev);

        if (wand == null || !TryComp<BasicEntityAmmoProviderComponent>(wand, out var basicAmmoComp) || basicAmmoComp.Count == null)
            return;

        _gunSystem.UpdateBasicEntityAmmoCount(wand.Value, basicAmmoComp.Count.Value + ev.Charge, basicAmmoComp);
    }
    // End Charge Spells
    #endregion
    #region Global Spells

    // TODO: Change this into a "StartRuleAction" when actions with multiple events are supported
    protected virtual void OnRandomGlobalSpawnSpell(RandomGlobalSpawnSpellEvent ev)
    {
        if (!_net.IsServer || ev.Handled || !PassesSpellPrerequisites(ev.Action, ev.Performer) || ev.Spawns is not { } spawns)
            return;

        ev.Handled = true;
        Speak(ev);

        var allHumans = _mind.GetAliveHumans();

        foreach (var human in allHumans)
        {
            if (!human.Comp.OwnedEntity.HasValue)
                continue;

            var ent = human.Comp.OwnedEntity.Value;

            if (_tag.HasTag(ent, "InvalidForGlobalSpawnSpell"))
                continue;

            var mapCoords = _transform.GetMapCoordinates(ent);
            foreach (var spawn in EntitySpawnCollection.GetSpawns(spawns, _random))
            {
                var spawned = Spawn(spawn, mapCoords);
                _hands.PickupOrDrop(ent, spawned);
            }
        }

        _audio.PlayGlobal(ev.Sound, ev.Performer);
    }

    #endregion
    #region Mindswap Spells

    private void OnMindSwapSpell(MindSwapSpellEvent ev)
    {
        if (ev.Handled || !PassesSpellPrerequisites(ev.Action, ev.Performer))
            return;

        ev.Handled = true;
        Speak(ev);

        // Need performer mind, but target mind is unnecessary, such as taking over a NPC
        // Need to get target mind before putting performer mind into their body if they have one
        // Thus, assign bool before first transfer, then check afterwards

        if (!_mind.TryGetMind(ev.Performer, out var perMind, out var perMindComp))
            return;

        var tarHasMind = _mind.TryGetMind(ev.Target, out var tarMind, out var tarMindComp);

        _mind.TransferTo(perMind, ev.Target);

        if (tarHasMind)
        {
            _mind.TransferTo(tarMind, ev.Performer);
        }

        _stun.TryParalyze(ev.Target, ev.TargetStunDuration, true);
        _stun.TryParalyze(ev.Performer, ev.PerformerStunDuration, true);
    }

    #endregion
    // End Spells
    #endregion

    // When any spell is cast it will raise this as an event, so then it can be played in server or something. At least until chat gets moved to shared
    // TODO: Temp until chat is in shared
    private void Speak(BaseActionEvent args)
    {
        if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
            return;

        var ev = new SpeakSpellEvent(args.Performer, speak.Speech);
        RaiseLocalEvent(ref ev);
    }
}
