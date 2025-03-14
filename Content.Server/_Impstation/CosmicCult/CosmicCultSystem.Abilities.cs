using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Maps;
using Robust.Shared.Map.Components;
using System.Numerics;
using Content.Server.Station.Components;
using Content.Server.Bible.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Content.Shared.Mind.Components;
using System.Collections.Immutable;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Content.Server.Light.Components;
using Content.Shared._Impstation.Cosmiccult;
using Robust.Shared.Physics.Events;
using Content.Shared.NPC;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Doors.Components;
using Robust.Shared.Player;
using Content.Shared.Physics;
using System.Linq;
using Content.Shared.StatusEffect;
using Timer = Robust.Shared.Timing.Timer;
using Robust.Shared.Map;
using Robust.Shared.Spawners;

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<CosmicImposingComponent, BeforeDamageChangedEvent>(OnImpositionDamaged);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicIngress>(OnCosmicIngress);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicGlare>(OnCosmicGlare);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicNova>(OnCosmicNova);
        SubscribeLocalEvent<CosmicAstralNovaComponent, StartCollideEvent>(OnNovaCollide);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicImposition>(OnCosmicImposition);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphon>(OnCosmicSiphon);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphonDoAfter>(OnCosmicSiphonDoAfter);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlankDoAfter>(OnCosmicBlankDoAfter);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicBlank>(OnCosmicBlank);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicLapse>(OnCosmicLapse);
        SubscribeLocalEvent<CosmicCultLeadComponent, EventCosmicPlaceMonument>(OnCosmicPlaceMonument);
        SubscribeLocalEvent<CosmicCultLeadComponent, EventCosmicMoveMonument>(OnCosmicMoveMonument);
        SubscribeLocalEvent<CosmicAstralBodyComponent, EventCosmicReturn>(OnCosmicReturn);
    }

    private void MalignEcho(Entity<CosmicCultComponent> uid)
    {
        if (_cultRule.CurrentTier > 1 && !_random.Prob(0.5f))
            Spawn("CosmicEchoVfx", Transform(uid).Coordinates);
    }

    #region Force Ingress
    private void OnCosmicIngress(Entity<CosmicCultComponent> uid, ref EventCosmicIngress args)
    {
        var target = args.Target;
        if (args.Handled)
            return;

        args.Handled = true;
        if (uid.Comp.CosmicEmpowered)
            if (TryComp<DoorBoltComponent>(target, out var doorBolt))
                _door.SetBoltsDown((target, doorBolt), false);
        _door.StartOpening(target);
        _audio.PlayPvs(uid.Comp.IngressSFX, uid);
        Spawn(uid.Comp.AbsorbVFX, Transform(target).Coordinates);
        MalignEcho(uid);
    }

    #endregion

    #region Null Glare
    private void OnCosmicGlare(Entity<CosmicCultComponent> uid, ref EventCosmicGlare args)
    {
        _audio.PlayPvs(uid.Comp.GlareSFX, uid);
        Spawn(uid.Comp.GlareVFX, Transform(uid).Coordinates);
        MalignEcho(uid);
        args.Handled = true;
        var entities = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, uid.Comp.CosmicGlareRange);
        entities.RemoveWhere(entity => !HasComp<PoweredLightComponent>(entity));
        foreach (var entity in entities)
            _poweredLight.TryDestroyBulb(entity);

        var targetFilter = Filter.Pvs(uid).RemoveWhere(player =>
        {
            if (player.AttachedEntity == null)
                return true;
            var ent = player.AttachedEntity.Value;
            if (!HasComp<MobStateComponent>(ent) || !HasComp<HumanoidAppearanceComponent>(ent) || HasComp<CosmicCultComponent>(ent) || HasComp<BibleUserComponent>(ent))
                return true;
            return !_interact.InRangeUnobstructed((uid, Transform(uid)), (ent, Transform(ent)), range: 0, collisionMask: CollisionGroup.Impassable);
        });
        var targets = new HashSet<NetEntity>(targetFilter.RemovePlayerByAttachedEntity(uid).Recipients.Select(ply => GetNetEntity(ply.AttachedEntity!.Value)));
        foreach (var target in targets)
        {
            _flash.Flash(GetEntity(target), uid, args.Action, uid.Comp.CosmicGlareDuration, uid.Comp.CosmicGlarePenalty, false, false, uid.Comp.CosmicGlareStun);
            _color.RaiseEffect(Color.CadetBlue, new List<EntityUid>() { GetEntity(target) }, Filter.Pvs(GetEntity(target), entityManager: EntityManager));
        }
    }
    #endregion

    #region Astral Nova
    /// <summary>
    /// This is the basic spell projectile code but updated to use non-obsolete functions, all so i can change the default projectile speed. Fuck.
    /// </summary>
    private void OnCosmicNova(Entity<CosmicCultComponent> uid, ref EventCosmicNova args)
    {
        var startPos = _transform.GetMapCoordinates(args.Performer);
        var targetPos = _transform.ToMapCoordinates(args.Target);
        var userVelocity = _physics.GetMapLinearVelocity(args.Performer);

        var delta = targetPos.Position - startPos.Position;
        if (delta.EqualsApprox(Vector2.Zero))
            delta = new(.01f, 0);

        args.Handled = true;
        var ent = Spawn("ProjectileCosmicNova", startPos);
        _gun.ShootProjectile(ent, delta, userVelocity, args.Performer, args.Performer, 5f);
        _audio.PlayPvs(uid.Comp.NovaCastSFX, uid, AudioParams.Default.WithVariation(0.1f));
        MalignEcho(uid);
    }

    private void OnNovaCollide(Entity<CosmicAstralNovaComponent> uid, ref StartCollideEvent args)
    {
        if (HasComp<CosmicCultComponent>(args.OtherEntity) || HasComp<BibleUserComponent>(args.OtherEntity) || !HasComp<MobStateComponent>(args.OtherEntity))
            return;

        _stun.TryParalyze(args.OtherEntity, TimeSpan.FromSeconds(2f), false);
        _damageable.TryChangeDamage(args.OtherEntity, uid.Comp.CosmicNovaDamage); // This'll probably trigger two or three times because of how collision works. I'm not being lazy here, it's a feature (kinda /s)
        _color.RaiseEffect(Color.Red, new List<EntityUid>() { args.OtherEntity }, Filter.Pvs(args.OtherEntity, entityManager: EntityManager));
    }
    #endregion

    #region Vacuous Imposition
    private void OnCosmicImposition(Entity<CosmicCultComponent> uid, ref EventCosmicImposition args)
    {
        EnsureComp<CosmicImposingComponent>(uid, out var comp);
        Timer.Spawn(uid.Comp.CosmicImpositionDuration, () => RemComp(uid, comp));
        Spawn(uid.Comp.ImpositionVFX, Transform(uid).Coordinates);
        args.Handled = true;
        _audio.PlayPvs(uid.Comp.ImpositionSFX, uid, AudioParams.Default.WithVariation(0.05f));
        MalignEcho(uid);
    }

    private void OnImpositionDamaged(Entity<CosmicImposingComponent> uid, ref BeforeDamageChangedEvent args)
    {
        args.Cancelled = true;
    }
    #endregion

    #region Siphon Entropy
    private void OnCosmicSiphon(Entity<CosmicCultComponent> uid, ref EventCosmicSiphon args)
    {
        if (HasComp<CosmicCultComponent>(args.Target) || HasComp<BibleUserComponent>(args.Target) || HasComp<ActiveNPCComponent>(args.Target) || TryComp<MobStateComponent>(args.Target, out var state) && state.CurrentState != MobState.Alive)
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-siphon-fail", ("target", Identity.Entity(args.Target, EntityManager))), uid, uid);
            return;
        }
        if (args.Handled)
            return;

        var doargs = new DoAfterArgs(EntityManager, uid, uid.Comp.CosmicSiphonDelay, new EventCosmicSiphonDoAfter(), uid, args.Target)
        {
            DistanceThreshold = 2f,
            Hidden = true,
            BreakOnHandChange = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
        };
        args.Handled = true;
        _doAfter.TryStartDoAfter(doargs);
    }

    private void OnCosmicSiphonDoAfter(Entity<CosmicCultComponent> uid, ref EventCosmicSiphonDoAfter args)
    {
        if (args.Args.Target == null)
            return;
        var target = args.Args.Target.Value;
        if (args.Cancelled || args.Handled)
            return;
        args.Handled = true;

        if (_mind.TryGetMind(uid, out var _, out var mind) && mind.Session != null)
            RaiseNetworkEvent(new CosmicSiphonIndicatorEvent(GetNetEntity(target!)), mind.Session);

        var entropyMote1 = _stack.Spawn(uid.Comp.CosmicSiphonQuantity, "Entropy", Transform(uid).Coordinates);
        _statusEffects.TryAddStatusEffect<CosmicEntropyDebuffComponent>(target, "EntropicDegen", TimeSpan.FromSeconds(21), true);
        _popup.PopupEntity(Loc.GetString("cosmicability-siphon-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        _hands.TryForcePickupAnyHand(uid, entropyMote1);
        _cultRule.IncrementCultObjectiveEntropy(uid);

        if (uid.Comp.CosmicEmpowered) // if you're empowered there's a 50% chance to flicker lights on siphon
        {
            var lights = GetEntityQuery<PoweredLightComponent>();
            foreach (var light in _lookup.GetEntitiesInRange(uid, 5, LookupFlags.StaticSundries)) // static range of 5. because.
            {
                if (!lights.HasComponent(light))
                    continue;
                if (!_random.Prob(0.5f))
                    continue;
                _ghost.DoGhostBooEvent(light);
            }
        }
    }
    #endregion

    #region "Shunt" Stun
    private void OnCosmicBlank(Entity<CosmicCultComponent> uid, ref EventCosmicBlank args)
    {
        if (HasComp<CosmicCultComponent>(args.Target) || HasComp<CosmicMarkBlankComponent>(args.Target) || HasComp<BibleUserComponent>(args.Target) || HasComp<ActiveNPCComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-generic-fail"), uid, uid);
            return;
        }
        if (args.Handled)
            return;

        var doargs = new DoAfterArgs(EntityManager, uid, uid.Comp.CosmicBlankDelay, new EventCosmicBlankDoAfter(), uid, args.Target)
        {
            DistanceThreshold = 1.5f,
            Hidden = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
        };
        args.Handled = true;
        _doAfter.TryStartDoAfter(doargs);
        _popup.PopupEntity(Loc.GetString("cosmicability-blank-begin", ("target", Identity.Entity(uid, EntityManager))), uid, args.Target);
    }

    private void OnCosmicBlankDoAfter(Entity<CosmicCultComponent> uid, ref EventCosmicBlankDoAfter args)
    {
        if (args.Args.Target == null)
            return;
        var target = args.Args.Target.Value;
        if (args.Cancelled || args.Handled)
            return;
        args.Handled = true;

        if (!TryComp<MindContainerComponent>(target, out var mindContainer) || !mindContainer.HasMind)
        {
            return;
        }

        EnsureComp<CosmicMarkBlankComponent>(target);
        _popup.PopupEntity(Loc.GetString("cosmicability-blank-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        var tgtpos = Transform(target).Coordinates;
        var mindEnt = mindContainer.Mind.Value;
        var mind = Comp<MindComponent>(mindEnt);
        var comp = uid.Comp;
        mind.PreventGhosting = true;

        var spawnPoints = EntityManager.GetAllComponents(typeof(CosmicVoidSpawnComponent)).ToImmutableList();
        if (spawnPoints.IsEmpty)
        {
            return;
        }
        _audio.PlayPvs(comp.BlankSFX, uid, AudioParams.Default.WithVolume(6f));
        Spawn(comp.BlankVFX, tgtpos);
        var newSpawn = _random.Pick(spawnPoints);
        var spawnTgt = Transform(newSpawn.Uid).Coordinates;
        var mobUid = Spawn(comp.SpawnWisp, spawnTgt);
        EnsureComp<InVoidComponent>(mobUid, out var inVoid);
        inVoid.OriginalBody = target;
        inVoid.ExitVoidTime = _timing.CurTime + comp.CosmicBlankDuration;
        _mind.TransferTo(mindEnt, mobUid);
        _stun.TryKnockdown(target, comp.CosmicBlankDuration + TimeSpan.FromSeconds(2), true);
        _popup.PopupEntity(Loc.GetString("cosmicability-blank-transfer"), mobUid, mobUid);
        _audio.PlayPvs(comp.BlankSFX, spawnTgt, AudioParams.Default.WithVolume(6f));
        _color.RaiseEffect(Color.CadetBlue, new List<EntityUid>() { target }, Filter.Pvs(target, entityManager: EntityManager));
        Spawn(comp.BlankVFX, spawnTgt);
        MalignEcho(uid);
    }
    #endregion

    #region "Lapse" Polymorph
    private void OnCosmicLapse(Entity<CosmicCultComponent> uid, ref EventCosmicLapse action)
    {
        if (action.Handled || HasComp<CosmicMarkBlankComponent>(action.Target) || HasComp<CleanseCultComponent>(action.Target) || HasComp<BibleUserComponent>(action.Target))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-generic-fail"), uid, uid);
            return;
        }
        action.Handled = true;
        var tgtpos = Transform(action.Target).Coordinates;
        Spawn(uid.Comp.LapseVFX, tgtpos);
        _popup.PopupEntity(Loc.GetString("cosmicability-lapse-success", ("target", Identity.Entity(action.Target, EntityManager))), uid, uid);
        TryComp<HumanoidAppearanceComponent>(action.Target, out HumanoidAppearanceComponent? species);
        switch (species!.Species) // We use a switch case for all the species polymorphs. Why? It uses wizden code, leans on YML, and it could be worse.
        {
            case "Human":
                _polymorph.PolymorphEntity(action.Target, "CosmicLapseMobHuman");
                break;
            case "Arachnid":
                _polymorph.PolymorphEntity(action.Target, "CosmicLapseMobArachnid");
                break;
            case "Diona":
                _polymorph.PolymorphEntity(action.Target, "CosmicLapseMobDiona");
                break;
            case "Moth":
                _polymorph.PolymorphEntity(action.Target, "CosmicLapseMobMoth");
                break;
            case "Vox":
                _polymorph.PolymorphEntity(action.Target, "CosmicLapseMobVox");
                break;
            case "Gastropoid":
                _polymorph.PolymorphEntity(action.Target, "CosmicLapseMobSnail");
                break;
            case "Decapoid":
                _polymorph.PolymorphEntity(action.Target, "CosmicLapseMobDecapoid");
                break;
            default:
                _polymorph.PolymorphEntity(action.Target, "CosmicLapseMobHuman");
                break;
        }
        MalignEcho(uid);
    }
    #endregion

    #region MonumentSpawn & MonumentMove
    //todo attack this with a debugger at some point, it seems to un-prime before it should sometimes?
    //no idea why, might be something to do with verifying placement inside the action's execution instead of in an attemptEvent beforehand?
    //yeah it is - if the action is primed but fails at this step, then the action becomes un-primed but does not properly go through, requiring it to be primed again
    //works fine:tm: for now with a slightly jank fix on the client end of things, will probably want to dig deeper?
    //actually might not want to fix it?
    //I've got the client stuff working well & this works out to making the ghost stay up so long as you consistently try (& fail) to place the monument
    //guess I should ask for specific feedback for this one tiny feature?
    private void OnCosmicPlaceMonument(Entity<CosmicCultLeadComponent> uid, ref EventCosmicPlaceMonument args)
    {
        if (!VerifyPlacement(uid, out var pos))
            return;

        _actions.RemoveAction(uid, uid.Comp.CosmicMonumentPlaceActionEntity);

        Spawn("MonumentCollider", pos);
        Spawn(uid.Comp.MonumentPrototype, pos);
    }

    private void OnCosmicMoveMonument(Entity<CosmicCultLeadComponent> uid, ref EventCosmicMoveMonument args)
    {

        if (!VerifyPlacement(uid, out var pos))
            return;

        _actions.RemoveAction(uid, uid.Comp.CosmicMonumentMoveActionEntity);

        //delete all old monument colliders for 100% safety
        var colliderQuery = EntityQueryEnumerator<MonumentCollisionComponent>();
        while (colliderQuery.MoveNext(out var collider, out _))
        {
            QueueDel(collider);
        }

        //spawn the destination effect first because we only need one
        var destEnt = Spawn("MonumentCosmicCultMoveEnd", pos);
        var destComp = EnsureComp<MonumentMoveDestinationComponent>(destEnt);
        Spawn("MonumentCollider", pos); //spawn a new collider

        //spawn the source effects & collider
        var monumentQuery = EntityQueryEnumerator<MonumentComponent>();
        while (monumentQuery.MoveNext(out var monument, out _))
        {
            Spawn("MonumentCosmicCultMoveStart", Transform(monument).Coordinates);
            Spawn("MonumentCollider", Transform(monument).Coordinates); //spawn a new collider
        }

        //timers!
        //move the monument to cheese world (the storage map)
        //these timers aren't amazing because they're basically magic values but eh
        Timer.Spawn(TimeSpan.FromSeconds(0.45),
            () =>
            {
                var monumentQuery = EntityQueryEnumerator<MonumentComponent>();
                while (monumentQuery.MoveNext(out var monument, out var monumentComp))
                {
                    //todo check if anything gets messed up by doing this to the monument?
                    _transform.SetParent(monument, EnsureStorageMapExists());
                    destComp.Monument = monument; //only get the first monument

                    if (monumentComp.CurrentGlyph is not null) //delete the scribed glyph as well
                        QueueDel(monumentComp.CurrentGlyph);

                    //close the UI for everyone who has it open
                    if (TryComp<UserInterfaceComponent>(uid, out var uiComp))
                    {
                        _ui.CloseUi((uid.Owner, uiComp), MonumentKey.Key);
                    }
                }

                //retrieve the monument from cheese world
                Timer.Spawn(TimeSpan.FromSeconds(0.8), //relative to the last timer
                    () =>
                    {
                        var colliderQuery = EntityQueryEnumerator<MonumentCollisionComponent>();
                        while (colliderQuery.MoveNext(out var collider, out _))
                        {
                            QueueDel(collider);
                        }

                        if (destComp.Monument is null)
                            return;

                        var xform = Transform(destEnt);
                        _transform.SetCoordinates(destComp.Monument.Value, xform.Coordinates);
                        _transform.AnchorEntity(destComp.Monument.Value); //no idea if this does anything but let's be safe about it
                        Spawn("MonumentCollider", xform.Coordinates);
                    });
            });
    }

    private EntityUid EnsureStorageMapExists()
    {
        if (_monumentStorageMap != null && Exists(_monumentStorageMap))
            return _monumentStorageMap.Value;

        _monumentStorageMap = _map.CreateMap();
        _map.SetPaused(_monumentStorageMap.Value, true);
        return _monumentStorageMap.Value;
    }

    //todo this can probably be mostly moved to shared but my brain isn't cooperating w/ that rn
    private bool VerifyPlacement(Entity<CosmicCultLeadComponent> uid, out EntityCoordinates outPos)
    {
        //MAKE SURE WE'RE STANDING ON A GRID
        var xform = Transform(uid);
        outPos = new EntityCoordinates();

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-grid"), uid, uid);
            return false;
        }

        var localTile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        var targetIndices = localTile.GridIndices + new Vector2i(0, 1);
        var pos = _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid);
        outPos = pos;
        var box = new Box2(pos.Position + new Vector2(-1.4f, -0.4f), pos.Position + new Vector2(1.4f, 0.4f));

        //CHECK IF IT'S BEING PLACED CHEESILY CLOSE TO SPACE
        var spaceDistance = 3;
        var worldPos = _transform.GetWorldPosition(xform);
        foreach (var tile in _map.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, spaceDistance)))
        {
            if (tile.IsSpace(_tileDef))
            {
                _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-space", ("DISTANCE", spaceDistance)), uid, uid);
                return false;
            }
        }

        //CHECK IF WE'RE ON THE STATION OR IF SOMEONE'S TRYING TO SNEAK THIS ONTO SOMETHING SMOL
        var station = _station.GetStationInMap(xform.MapID);

        EntityUid? stationGrid = null;

        if (TryComp<StationDataComponent>(station, out var stationData))
            stationGrid = _station.GetLargestGrid(stationData);

        if (stationGrid is not null && stationGrid != xform.GridUid)
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-station"), uid, uid);
            return false;
        }

        //CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS
        if (_lookup.AnyLocalEntitiesIntersecting(xform.GridUid.Value, box, LookupFlags.Dynamic | LookupFlags.Static, uid))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-intersection"), uid, uid);
            return false;
        }

        return true;
    }
    #endregion

    #region Return (for Glyph)
    private void OnCosmicReturn(Entity<CosmicAstralBodyComponent> uid, ref EventCosmicReturn args) //This action is exclusive to the Glyph-created Astral Projection, and allows the user to return to their original body.
    {
        if (_mind.TryGetMind(args.Performer, out var mindId, out var _))
            _mind.TransferTo(mindId, uid.Comp.OriginalBody);
        var mind = Comp<MindComponent>(mindId);
        mind.PreventGhosting = false;
        QueueDel(uid);
        RemComp<CosmicMarkBlankComponent>(uid.Comp.OriginalBody);
    }
    #endregion

}
