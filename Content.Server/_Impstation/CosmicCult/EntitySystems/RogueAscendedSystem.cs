using System.Numerics;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Server._Impstation.Thaven;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.Thaven;
using Content.Shared._Impstation.Thaven.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Dataset;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition;
using Content.Shared.Popups;
using Content.Shared.Random;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

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
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ThavenMoodsSystem _moodSystem = default!; //impstation

    [ValidatePrototypeId<DatasetPrototype>]
    private const string AscendantDataset = "ThavenMoodsAscendantInfection";

    [ValidatePrototypeId<WeightedRandomPrototype>]
    private const string RandomThavenMoodDataset = "RandomThavenMoodDataset";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RogueAscendedDendriteComponent, BeforeFullyEatenEvent>(OnDendriteConsumed);

        SubscribeLocalEvent<RogueAscendedComponent, EventRogueCosmicNova>(OnRogueNova);
        SubscribeLocalEvent<RogueAscendedComponent, EventRogueCosmicBeam>(OnRogueHyperbeam);

        SubscribeLocalEvent<HumanoidAppearanceComponent, EventRogueCosmicNova>(OnPlayerNova);
        SubscribeLocalEvent<HumanoidAppearanceComponent, EventRogueCosmicBeam>(OnPlayerHyperbeam);

        SubscribeLocalEvent<RogueAscendedComponent, EventRogueInfection>(OnAttemptInfection);
        SubscribeLocalEvent<RogueAscendedComponent, EventRogueInfectionDoAfter>(OnInfectionDoAfter);
        SubscribeLocalEvent<RogueAscendedInfectionComponent, ComponentShutdown>(OnInfectionCleansed);
    }






    #region Consume Dendrite
    private void OnDendriteConsumed(Entity<RogueAscendedDendriteComponent> uid, ref BeforeFullyEatenEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.User) || HasComp<RogueAscendedAuraComponent>(args.User)) return; // if it ain't human, or already ate, nvm
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
            _moodSystem.ToggleEmaggable(moodComp); // enable emagging again
            _moodSystem.ToggleSharedMoods(moodComp); // enable shared moods
            _moodSystem.ClearMoods(moodComp); // wipe those moods again
            _moodSystem.TryAddRandomMood(uid, RandomThavenMoodDataset, moodComp, false);
            _moodSystem.TryAddRandomMood(uid, moodComp);
        }
        else RemComp<ThavenMoodsComponent>(uid);
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
        if (args.Handled)
            return;

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
        _moodSystem.ToggleEmaggable(moodComp); // can't emag an infected thavenmood
        _moodSystem.ClearMoods(moodComp); // wipe those moods
        _moodSystem.ToggleSharedMoods(moodComp); // disable shared moods
        _moodSystem.TryAddRandomMood(target, AscendantDataset, moodComp, false); // we don't need to notify them twice
        _moodSystem.TryAddRandomMood(target, AscendantDataset, moodComp);
        _damageable.TryChangeDamage(target, uid.Comp.InfectionHeal * -1);
        _stun.TryStun(target, uid.Comp.StunTime, false);
        _audio.PlayPvs(uid.Comp.InfectionSfx, target);

        if (_mindSystem.TryGetObjectiveComp<RogueInfectionConditionComponent>(uid, out var obj))
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
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ability_nova_cast.ogg"), uid, AudioParams.Default.WithVariation(0.1f));
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
    #region Ability - 2nd
    private void CastHyperbeam(EntityUid uid, EventRogueCosmicBeam args)
    {

    }
    private void OnRogueHyperbeam(Entity<RogueAscendedComponent> uid, ref EventRogueCosmicBeam args)
    {
        // CastHyperbeam(uid, args);
    }
    private void OnPlayerHyperbeam(Entity<HumanoidAppearanceComponent> uid, ref EventRogueCosmicBeam args)
    {
        // CastHyperbeam(uid, args);
    }
    #endregion
}
