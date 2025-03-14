using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Server._Impstation.Thaven;
using Content.Server.Actions;
using Content.Server.Bible.Components;
using Content.Server.Interaction;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Popups;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Content.Shared._Impstation.Thaven.Components;
using Content.Shared.Damage;
using Content.Shared.Dataset;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.CosmicCult.EntitySystems;

public sealed class RogueAscendedSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly InteractionSystem _interact = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ThavenMoodsSystem _moodSystem = default!; //impstation

    [ValidatePrototypeId<DatasetPrototype>]
    private const string AscendantDataset = "ThavenMoodsAscendantInfection";

    [ValidatePrototypeId<WeightedRandomPrototype>]
    private const string RandomThavenMoodDataset = "RandomThavenMoodDataset";
    [DataField] private EntProtoId _spawnWisp = "MobCosmicWisp";
    [DataField] private EntProtoId _blankVFX = "CosmicBlankAbilityVFX";
    [DataField] private EntProtoId _glareVFX = "CosmicGlareAbilityVFX";
    [DataField] private SoundSpecifier _blankSFX = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ability_blank.ogg");
    [DataField] private SoundSpecifier _ascendantSFX = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ascendant_shunt.ogg");
    [DataField] private SoundSpecifier _novaSFX = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ability_nova_cast.ogg");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RogueAscendedComponent, ComponentInit>(OnSpawn);
        SubscribeLocalEvent<RogueAscendedComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RogueAscendedDendriteComponent, BeforeFullyEatenEvent>(OnDendriteConsumed);

        SubscribeLocalEvent<RogueAscendedComponent, EventRogueShatter>(OnRogueShatter);
        SubscribeLocalEvent<RogueAscendedComponent, EventRogueGrandShunt>(OnRogueShunt);
        SubscribeLocalEvent<RogueAscendedComponent, EventRogueCosmicNova>(OnRogueNova);
        SubscribeLocalEvent<HumanoidAppearanceComponent, EventRogueCosmicNova>(OnPlayerNova);

        SubscribeLocalEvent<RogueAscendedComponent, EventRogueInfection>(OnAttemptInfection);
        SubscribeLocalEvent<RogueAscendedComponent, EventRogueInfectionDoAfter>(OnInfectionDoAfter);
        SubscribeLocalEvent<RogueAscendedInfectionComponent, ComponentShutdown>(OnInfectionCleansed);
    }
    #region Spawn
    private void OnSpawn(Entity<RogueAscendedComponent> uid, ref ComponentInit args) // I WANT THIS DINGUS YEETED TOWARDS THE STATION AT MACH JESUS
    {
        var station = _station.GetStationInMap(Transform(uid).MapID);
        if (TryComp<StationDataComponent>(station, out var stationData))
        {
            var stationGrid = _station.GetLargestGrid(stationData);
            _throw.TryThrow(uid, Transform(stationGrid!.Value).Coordinates, baseThrowSpeed: 50, null, 0, 0, false, false, false, false, false);
        }
    }
    #endregion

    #region Death
    private void OnMobStateChanged(EntityUid uid, RogueAscendedComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;
        _audio.PlayPvs(comp.MobSound, uid);
    }
    #endregion

    #region Consume Dendrite
    private void OnDendriteConsumed(Entity<RogueAscendedDendriteComponent> uid, ref BeforeFullyEatenEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.User) || HasComp<RogueAscendedAuraComponent>(args.User))
            return; // if it ain't human, or already ate, nvm
        if (TryComp<CosmicCultComponent>(args.User, out var cultComp))
        {
            cultComp.EntropyBudget += 30; //if they're a cultist, make them very very rich
            cultComp.CosmicEmpowered = true; // also empower them, assuming they aren't already
            return;
        }
        Spawn(uid.Comp.Vfx, Transform(args.User).Coordinates);
        EnsureComp<RogueAscendedAuraComponent>(args.User, out var starMark);
        _actions.AddAction(args.User, ref uid.Comp.RogueFoodActionEntity, uid.Comp.RogueFoodAction, args.User);
        _audio.PlayPvs(uid.Comp.ActivateSfx, args.User);
        _popup.PopupCoordinates(Loc.GetString("rogue-ascended-dendrite-eaten"), Transform(args.User).Coordinates, PopupType.Medium);
        _color.RaiseEffect(Color.CadetBlue, new List<EntityUid>() { args.User }, Filter.Pvs(args.User, entityManager: EntityManager));
        _stun.TryKnockdown(args.User, uid.Comp.StunTime, false);
        Dirty(args.User, starMark);
    }
    #endregion
    #region Cleanse
    private void OnInfectionCleansed(Entity<RogueAscendedInfectionComponent> uid, ref ComponentShutdown args)
    {
        if (uid.Comp.HadMoods)
        {
            EnsureComp<ThavenMoodsComponent>(uid, out var moodComp); // ensure it because we don't need another if()
            _moodSystem.ToggleEmaggable((uid, moodComp)); // enable emagging again
            _moodSystem.ToggleSharedMoods((uid, moodComp)); // enable shared moods
            _moodSystem.ClearMoods((uid, moodComp)); // wipe those moods again
            _moodSystem.TryAddRandomMood((uid, moodComp), false);
            _moodSystem.TryAddRandomMood((uid, moodComp));
        }
        else
            RemComp<ThavenMoodsComponent>(uid);
    }
    #endregion
    #region Ability - Shatter
    private void OnRogueShatter(Entity<RogueAscendedComponent> uid, ref EventRogueShatter args)
    {
        if (TryComp<MobStateComponent>(args.Target, out var state) && state.CurrentState != MobState.Alive)
        {
            _popup.PopupEntity(Loc.GetString("rogue-ascended-shatter-fail"), uid, uid);
            return;
        }
        if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Critical, out var damage))
            return;
        DamageSpecifier dspec = new();
        dspec.DamageDict.Add("Cold", damage.Value);
        _damageable.TryChangeDamage(args.Target, dspec, true, origin: uid);
        _audio.PlayPvs(uid.Comp.ShatterSfx, args.Target);
        args.Handled = true;
        Spawn(uid.Comp.Vfx, Transform(args.Target).Coordinates);
    }
    #endregion
    #region Ability - Infection
    private void OnAttemptInfection(Entity<RogueAscendedComponent> uid, ref EventRogueInfection args)
    {
        if (HasComp<RogueAscendedInfectionComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("rogue-ascended-infection-alreadyinfected", ("target", Identity.Entity(args.Target, EntityManager))), uid, uid);
            return;
        }
        if (TryComp<MobStateComponent>(args.Target, out var state) && state.CurrentState != MobState.Critical)
        {
            _popup.PopupEntity(Loc.GetString("rogue-ascended-infection-fail"), uid, uid);
            return;
        }
        if (args.Handled || !TryComp<MindContainerComponent>(args.Target, out var mindContainer) || !mindContainer.HasMind)
        {
            _popup.PopupEntity(Loc.GetString("rogue-ascended-infection-error"), uid, uid);
            return;
        }
        var doargs = new DoAfterArgs(EntityManager, uid, uid.Comp.RogueInfectionTime, new EventRogueInfectionDoAfter(), uid, args.Target)
        {
            DistanceThreshold = 2f,
            Hidden = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            BlockDuplicate = true,
        };
        args.Handled = true;
        _doAfter.TryStartDoAfter(doargs);
        _audio.PlayPvs(uid.Comp.MobSound, uid);
        _popup.PopupCoordinates(Loc.GetString("rogue-ascended-infection-notification",
        ("target", Identity.Entity(args.Target, EntityManager)),
        ("user", Identity.Entity(args.Performer, EntityManager))),
        Transform(uid).Coordinates, PopupType.LargeCaution);
    }
    private void OnInfectionDoAfter(Entity<RogueAscendedComponent> uid, ref EventRogueInfectionDoAfter args)
    {
        if (args.Cancelled || args.Target == null)
            return;
        var target = args.Target.Value;
        EnsureComp<RogueAscendedInfectionComponent>(target, out var infectionComp);
        if (HasComp<ThavenMoodsComponent>(target))
            infectionComp.HadMoods = true; // make note that they already had moods
        EnsureComp<ThavenMoodsComponent>(target, out var moodComp);
        Spawn(uid.Comp.Vfx, Transform(target).Coordinates);

        _moodSystem.ToggleEmaggable((target, moodComp)); // can't emag an infected thavenmood
        _moodSystem.ClearMoods((target, moodComp)); // wipe those moods
        _moodSystem.ToggleSharedMoods((target, moodComp)); // disable shared moods
        _moodSystem.TryAddRandomMood((target, moodComp), AscendantDataset, false); // we don't need to notify them twice
        _moodSystem.TryAddRandomMood((target, moodComp), AscendantDataset);

        _damageable.TryChangeDamage(target, uid.Comp.InfectionHeal * -1);

        _stun.TryStun(target, uid.Comp.StunTime, false);
        _audio.PlayPvs(uid.Comp.InfectionSfx, target);

        if (_mind.TryGetObjectiveComp<RogueInfectionConditionComponent>(uid, out var obj))
            obj.MindsCorrupted++;
    } // the year is 2093. We invoke 5,922 systems and add 30,419 components to an entity. Beacuase.
    #endregion
    #region Ability - Nova
    private void CastNova(EntityUid uid, EventRogueCosmicNova args)
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
        _audio.PlayPvs(_novaSFX, uid, AudioParams.Default.WithVariation(0.1f));
    }
    private void OnRogueNova(Entity<RogueAscendedComponent> uid, ref EventRogueCosmicNova args)
    {
        CastNova(uid, args);
    }
    private void OnPlayerNova(Entity<HumanoidAppearanceComponent> uid, ref EventRogueCosmicNova args)
    {
        CastNova(uid, args);
    }
    #endregion
    #region Ability - GrandShunt
    private void OnRogueShunt(Entity<RogueAscendedComponent> uid, ref EventRogueGrandShunt args)
    {
        args.Handled = true;
        Spawn(_glareVFX, Transform(uid).Coordinates);
        var entities = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 10);
        entities.RemoveWhere(entity => !HasComp<PoweredLightComponent>(entity));
        foreach (var entity in entities)
            _poweredLight.TryDestroyBulb(entity);

        var targetFilter = Filter.Pvs(uid).RemoveWhere(player =>
        {
            if (player.AttachedEntity == null)
                return true;
            var ent = player.AttachedEntity.Value;
            if (!HasComp<MobStateComponent>(ent) || !HasComp<HumanoidAppearanceComponent>(ent) || HasComp<BibleUserComponent>(ent))
                return true;
            return !_interact.InRangeUnobstructed((uid, Transform(uid)), (ent, Transform(ent)), range: 0, collisionMask: CollisionGroup.Impassable);
        });
        var targets = new HashSet<NetEntity>(targetFilter.RemovePlayerByAttachedEntity(uid).Recipients.Select(ply => GetNetEntity(ply.AttachedEntity!.Value)));
        foreach (var target in targets)
        {
            var subject = GetEntity(target);
            if (!TryComp<MindContainerComponent>(subject, out var mindContainer) || !mindContainer.HasMind)
            {
                return;
            }
            var tgtpos = Transform(subject).Coordinates;
            var mindEnt = mindContainer.Mind.Value;
            var mind = Comp<MindComponent>(mindEnt);
            mind.PreventGhosting = true;

            var spawnPoints = EntityManager.GetAllComponents(typeof(CosmicVoidSpawnComponent)).ToImmutableList();
            if (spawnPoints.IsEmpty)
            {
                return;
            }
            var newSpawn = _random.Pick(spawnPoints);
            var spawnTgt = Transform(newSpawn.Uid).Coordinates;
            var mobUid = Spawn(_spawnWisp, spawnTgt);

            EnsureComp<CosmicMarkBlankComponent>(subject);
            EnsureComp<InVoidComponent>(mobUid, out var inVoid);

            inVoid.OriginalBody = subject;
            inVoid.ExitVoidTime = _timing.CurTime + TimeSpan.FromSeconds(14);

            _mind.TransferTo(mindEnt, mobUid);
            _stun.TryKnockdown(subject, TimeSpan.FromSeconds(16), true);
            _popup.PopupEntity(Loc.GetString("cosmicability-blank-transfer"), mobUid, mobUid);
            _audio.PlayLocal(_blankSFX, mobUid, mobUid, AudioParams.Default.WithVolume(6f));
            _color.RaiseEffect(Color.CadetBlue, new List<EntityUid>() { subject }, Filter.Pvs(subject, entityManager: EntityManager));

            Spawn(_blankVFX, tgtpos);
            Spawn(_blankVFX, spawnTgt);
        }
        _audio.PlayPvs(_ascendantSFX, uid, AudioParams.Default.WithVolume(6f));
    }
    #endregion
}
