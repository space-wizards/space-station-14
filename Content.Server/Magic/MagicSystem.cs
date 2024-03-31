using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Magic.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Magic;
using Content.Shared.Magic.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Spawners;
using Content.Server.Emp;
using System.Diagnostics;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.NPC.HTN;
using Content.Shared.NPC.Systems;
using Content.Shared.Eye.Blinding.Systems;
using System.Threading.Tasks;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Explosion.Components;
using System.Threading;
using Content.Server.Explosion.EntitySystems;
using System.Transactions;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Mind;
using Content.Shared.StatusEffect;
using Content.Server.Polymorph.Systems;
using Content.Server.Chemistry.ReagentEffects;
using SQLitePCL;
using Content.Server;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Magic;

/// <summary>
/// Handles learning and using spells (actions)
/// </summary>
public sealed class MagicSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _seriMan = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly BlindableSystem _blindnessSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly DamageableSystem _damaging = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;



    public override void Initialize()
    {
        base.Initialize();



        SubscribeLocalEvent<SpellbookComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<SpellbookComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<SpellbookComponent, SpellbookDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<InstantSpawnSpellEvent>(OnInstantSpawn);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<KnockSpellEvent>(OnKnockSpell);
        SubscribeLocalEvent<SmiteSpellEvent>(OnSmiteSpell);
        SubscribeLocalEvent<WorldSpawnSpellEvent>(OnWorldSpawn);
        SubscribeLocalEvent<ProjectileSpellEvent>(OnProjectileSpell);
        SubscribeLocalEvent<ChangeComponentsSpellEvent>(OnChangeComponentsSpell);
        SubscribeLocalEvent<EMPSpellEvent>(OnEMPSpell);
        SubscribeLocalEvent<AnimateDeadSpellEvent>(OnAnimateDead);
        SubscribeLocalEvent<BlindSpellEvent>(OnBlindSpell);
        SubscribeLocalEvent<SmokeSpellEvent>(OnSmokeSpell);
        SubscribeLocalEvent<MindswapSpellEvent>(OnMindswapSpell);
        SubscribeLocalEvent<SwapSpellEvent>(OnSwapSpell);
        SubscribeLocalEvent<ClownifySpellEvent>(OnClownifySpell);
        SubscribeLocalEvent<DevolveSpellEvent>(OnDevolveSpell);
        SubscribeLocalEvent<HealSpellEvent>(OnHealSpell);
    }

    private void OnDoAfter(EntityUid uid, SpellbookComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        if (!component.LearnPermanently)
        {
            _actionsSystem.GrantActions(args.Args.User, component.Spells, uid);
            return;
        }

        foreach (var (id, charges) in component.SpellActions)
        {
            // TOOD store spells entity ids on some sort of innate magic user component or something like that.
            EntityUid? actionId = null;
            if (_actionsSystem.AddAction(args.Args.User, ref actionId, id))
                _actionsSystem.SetCharges(actionId, charges < 0 ? null : charges);
        }

        component.SpellActions.Clear();
        if (component.DeleteAfterLearn)
        {
            _entityManager.DeleteEntity(uid);
        }
    }

    private void OnInit(EntityUid uid, SpellbookComponent component, MapInitEvent args)
    {
        if (component.LearnPermanently)
            return;

        foreach (var (id, charges) in component.SpellActions)
        {
            var spell = _actionContainer.AddAction(uid, id);
            if (spell == null)
                continue;

            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell.Value);
        }
    }


    private void OnUse(EntityUid uid, SpellbookComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        AttemptLearn(uid, component, args);

        args.Handled = true;
    }

    private void AttemptLearn(EntityUid uid, SpellbookComponent component, UseInHandEvent args)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.LearnTime, new SpellbookDoAfterEvent(), uid, target: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true //What, are you going to read with your eyes only??
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    #region Spells

    /// <summary>
    /// Handles the instant action (i.e. on the caster) attempting to spawn an entity.
    /// </summary>
    private void OnInstantSpawn(InstantSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        foreach (var position in GetSpawnPositions(transform, args.Pos))
        {
            var ent = Spawn(args.Prototype, position.SnapToGrid(EntityManager, _mapManager));

            if (args.PreventCollideWithCaster)
            {
                var comp = EnsureComp<PreventCollideComponent>(ent);
                comp.Uid = args.Performer;
            }
        }

        Speak(args);
        args.Handled = true;
    }

    private void OnEMPSpell(EMPSpellEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Speak(args);

        //Get the position of the player
        var transform = Transform(args.Performer);
        var coords = transform.MapPosition;

        _audio.PlayPvs(args.EmpSound, args.Performer, AudioParams.Default.WithVolume(args.EmpVolume));

        _emp.EmpPulse(coords, 4f, 50000, 10f);

    }

    private void OnProjectileSpell(ProjectileSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        var xform = Transform(ev.Performer);
        var userVelocity = _physics.GetMapLinearVelocity(ev.Performer);

        foreach (var pos in GetSpawnPositions(xform, ev.Pos))
        {
            // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
            var mapPos = pos.ToMap(EntityManager, _transformSystem);
            var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                ? pos.WithEntityId(gridUid, EntityManager)
                : new(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

            var ent = Spawn(ev.Prototype, spawnCoords);
            var direction = ev.Target.ToMapPos(EntityManager, _transformSystem) -
                            spawnCoords.ToMapPos(EntityManager, _transformSystem);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, ev.Performer, ev.Performer);
        }
    }

    private void OnChangeComponentsSpell(ChangeComponentsSpellEvent ev)
    {
        if (ev.Handled)
            return;
        ev.Handled = true;
        Speak(ev);

        foreach (var toRemove in ev.ToRemove)
        {
            if (_compFact.TryGetRegistration(toRemove, out var registration))
                RemComp(ev.Target, registration.Type);
        }

        foreach (var (name, data) in ev.ToAdd)
        {
            if (HasComp(ev.Target, data.Component.GetType()))
                continue;

            var component = (Component) _compFact.GetComponent(name);
            component.Owner = ev.Target;
            var temp = (object) component;
            _seriMan.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(ev.Target, (Component) temp!);
        }
    }

    private List<EntityCoordinates> GetSpawnPositions(TransformComponent casterXform, MagicSpawnData data)
    {
        switch (data)
        {
            case TargetCasterPos:
                return new List<EntityCoordinates>(1) { casterXform.Coordinates };
            case TargetInFront:
                {
                    // This is shit but you get the idea.
                    var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

                    if (!TryComp<MapGridComponent>(casterXform.GridUid, out var mapGrid))
                        return new List<EntityCoordinates>();

                    if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
                        return new List<EntityCoordinates>();

                    var tileIndex = tileReference.Value.GridIndices;
                    var coords = mapGrid.GridTileToLocal(tileIndex);
                    EntityCoordinates coordsPlus;
                    EntityCoordinates coordsMinus;

                    var dir = casterXform.LocalRotation.GetCardinalDir();
                    switch (dir)
                    {
                        case Direction.North:
                        case Direction.South:
                            {
                                coordsPlus = mapGrid.GridTileToLocal(tileIndex + (1, 0));
                                coordsMinus = mapGrid.GridTileToLocal(tileIndex + (-1, 0));
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
                                coordsPlus = mapGrid.GridTileToLocal(tileIndex + (0, 1));
                                coordsMinus = mapGrid.GridTileToLocal(tileIndex + (0, -1));
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

    /// <summary>
    /// Teleports the user to the clicked location
    /// </summary>
    /// <param name="args"></param>
    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        if (transform.MapID != args.Target.GetMapId(EntityManager)) return;

        _transformSystem.SetCoordinates(args.Performer, args.Target);
        transform.AttachToGridOrMap();
        _audio.PlayPvs(args.BlinkSound, args.Performer, AudioParams.Default.WithVolume(args.BlinkVolume));
        Speak(args);
        args.Handled = true;
    }

    /// <summary>
    /// Opens all doors within range
    /// </summary>
    /// <param name="args"></param>
    private void OnKnockSpell(KnockSpellEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Speak(args);

        //Get the position of the player
        var transform = Transform(args.Performer);
        var coords = transform.Coordinates;

        _audio.PlayPvs(args.KnockSound, args.Performer, AudioParams.Default.WithVolume(args.KnockVolume));

        //Look for doors and don't open them if they're already open.
        foreach (var entity in _lookup.GetEntitiesInRange(coords, args.Range))
        {
            if (TryComp<DoorBoltComponent>(entity, out var bolts))
                _doorSystem.SetBoltsDown((entity, bolts), false);

            if (TryComp<DoorComponent>(entity, out var doorComp) && doorComp.State is not DoorState.Open)
                _doorSystem.StartOpening(entity);
        }
    }

    private void OnSmiteSpell(SmiteSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        var direction = Transform(ev.Target).MapPosition.Position - Transform(ev.Performer).MapPosition.Position;
        var impulseVector = direction * 10000;

        _physics.ApplyLinearImpulse(ev.Target, impulseVector);

        if (!TryComp<BodyComponent>(ev.Target, out var body))
            return;

        var ents = _bodySystem.GibBody(ev.Target, true, body);

        if (!ev.DeleteNonBrainParts)
            return;

        foreach (var part in ents)
        {
            // just leaves a brain and clothes
            if (HasComp<BodyComponent>(part) && !HasComp<BrainComponent>(part))
            {
                QueueDel(part);
            }
        }
    }

    /// <summary>
    /// Spawns entity prototypes from a list within range of click.
    /// </summary>
    /// <remarks>
    /// It will offset mobs after the first mob based on the OffsetVector2 property supplied.
    /// </remarks>
    /// <param name="args"> The Spawn Spell Event args.</param>
    private void OnWorldSpawn(WorldSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var targetMapCoords = args.Target;

        SpawnSpellHelper(args.Contents, targetMapCoords, args.Lifetime, args.Offset);
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
    private void SpawnSpellHelper(List<EntitySpawnEntry> entityEntries, EntityCoordinates entityCoords, float? lifetime, Vector2 offsetVector2)
    {
        var getProtos = EntitySpawnCollection.GetSpawns(entityEntries, _random);

        var offsetCoords = entityCoords;
        foreach (var proto in getProtos)
        {
            // TODO: Share this code with instant because they're both doing similar things for positioning.
            var entity = Spawn(proto, offsetCoords);
            offsetCoords = offsetCoords.Offset(offsetVector2);

            if (lifetime != null)
            {
                var comp = EnsureComp<TimedDespawnComponent>(entity);
                comp.Lifetime = lifetime.Value;
            }
        }
    }

    private void OnAnimateDead(AnimateDeadSpellEvent args)
    {
        {
            if (args.Handled)
                return;

            args.Handled = true;
            Speak(args);

            //Get the position of the player
            var transform = Transform(args.Performer);
            var coords = transform.Coordinates;

            // _audio.PlayPvs(args.AnimateSound, args.Performer, AudioParams.Default.WithVolume(args.Anima));

            //Look for doors and don't open them if they're already open.
            _audio.PlayPvs(args.AnimateSound, args.Performer, AudioParams.Default.WithVolume(args.AnimateVolume));
            foreach (var entity in _lookup.GetEntitiesInRange(coords, args.Range))
            {
                if (!TryComp<MobStateComponent>(entity, out var mobState))
                    continue;
                if (_mobStateSystem.IsDead(entity))
                {
                    if (!TryComp<BodyComponent>(entity, out var body))
                        return;
                    _bodySystem.GibBody(entity, true, body);
                    var mobtransform = Transform(entity);
                    var mobcoords = mobtransform.MapPosition;
                    var mob = Spawn("MobSkeletonAngry", mobcoords);
                    _metaData.SetEntityName(mob, "angry skeleton");
                    _metaData.SetEntityDescription(mob, "This skeleton is out for blood");
                    _npcFactionSystem.AddFaction(mob, "SimpleHostile");
                    _npcFactionSystem.RemoveFaction(mob, "NanoTrasen", false);
                    _npcFactionSystem.IgnoreEntity(entity, args.Performer);
                    var comp = _entityManager.AddComponent<HTNComponent>(mob);
                    comp.RootTask = new HTNCompoundTask()
                    {
                        Task = "SimpleHostileCompound"
                    };
                }
            }
        }
    }

    private async void OnBlindSpell(BlindSpellEvent ev)
    {
        {
            if (ev.Handled)
                return;

            ev.Handled = true;
            Speak(ev);
            var comp = Comp<BlindableComponent>(ev.Target);
            var olddamage = comp.EyeDamage;
            _blindnessSystem.AdjustEyeDamage(ev.Target, 9);
            _blindnessSystem.UpdateIsBlind(ev.Target);
            await Task.Delay(10000);
            _blindnessSystem.AdjustEyeDamage(ev.Target, -(9 - olddamage));
            _blindnessSystem.UpdateIsBlind(ev.Target);
        }
    }

    private void OnSmokeSpell(SmokeSpellEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Speak(args);

        //Get the position of the player
        var transform = Transform(args.Performer);
        if (!_mapManager.TryFindGridAt(transform.MapPosition, out _, out var grid))
            return;
        // var coords = transform.MapPosition;
        var coords = grid.MapToGrid(transform.MapPosition);

        _audio.PlayPvs(args.SmokeSound, args.Performer, AudioParams.Default.WithVolume(args.SmokeVolume));
        var ent = Spawn("Smoke", coords.SnapToGrid());
        _smoke.StartSmoke(ent, new(), 15, 40);
    }

    public void OnMindswapSpell(MindswapSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);


        _statusEffects.TryAddStatusEffect(ev.Performer, "KnockedDown", TimeSpan.FromSeconds(5), true);
        _statusEffects.TryAddStatusEffect(ev.Target, "KnockedDown", TimeSpan.FromSeconds(5), true);
        //Get the position of the player
        if (!_mind.TryGetMind(ev.Performer, out var selfmindId, out var selfmind))
            return;

        _mind.ControlMob(ev.Performer, ev.Target);

        if (!_mind.TryGetMind(ev.Target, out var targetmindId, out var targetmind))
            return;

       _mind.ControlMob(ev.Target, ev.Performer);
       Console.WriteLine("mindswap lawd");

    }

    public void OnSwapSpell(SwapSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        var transform = Transform(ev.Performer);
        var coords = transform.Coordinates;

        var transformother = Transform(ev.Target);
        var coordsother = transformother.Coordinates;

        _transformSystem.SetCoordinates(ev.Performer, coordsother);
        _transformSystem.SetCoordinates(ev.Target, coords);
    }

    public void OnClownifySpell(ClownifySpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        _polymorph.PolymorphEntity(ev.Target, "ArtifactCluwne");
    }

    public void OnDevolveSpell(DevolveSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        _polymorph.PolymorphEntity(ev.Target, "ArtifactMonkey");
    }

    private void OnHealSpell(HealSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);
        var healBlunt = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), -30);
        var healSlash = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), -30);
        var healPierce = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Piercing"), -30);
        var healHeat = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Heat"), -30);
        var healCold = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Cold"), -30);
        var healPoison = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Poison"), -30);
        var healRadiation = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Radiation"), -30);
        var healAsphyxiation = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), -30);
        var healBloodloss = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Bloodloss"), -30);
        var healCell = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Cellular"), -30);
        _damaging.TryChangeDamage(ev.Target, healBlunt);
        _damaging.TryChangeDamage(ev.Target, healSlash);
        _damaging.TryChangeDamage(ev.Target, healPierce);
        _damaging.TryChangeDamage(ev.Target, healHeat);
        _damaging.TryChangeDamage(ev.Target, healCold);
        _damaging.TryChangeDamage(ev.Target, healPoison);
        _damaging.TryChangeDamage(ev.Target, healRadiation);
        _damaging.TryChangeDamage(ev.Target, healBloodloss);
        _damaging.TryChangeDamage(ev.Target, healAsphyxiation);
        _damaging.TryChangeDamage(ev.Target, healCell);
        _bloodstream.TryModifyBleedAmount(ev.Target, -100);
    }

    #endregion

    private void Speak(BaseActionEvent args)
    {
        if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
            return;

        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(speak.Speech),
            InGameICChatType.Speak, false);
    }
}
