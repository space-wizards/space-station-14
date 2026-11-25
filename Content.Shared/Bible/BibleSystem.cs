using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Bible.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Bible;

/// <summary>
/// Handles bible healing on hit, and summoning/respawning of familiars via <see cref="SummonableComponent"/>.
/// </summary>
public sealed partial class BibleSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UseDelaySystem _delay = default!;

    /// <summary>
    /// This handles familiar respawning.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SummonableRespawningComponent, SummonableComponent>();
        while (query.MoveNext(out var uid, out var respawningComponent, out var summonableComp))
        {
            if (summonableComp.RespawnTime > _timing.CurTime)
                continue;

            // Delete old summoned entity.
            if (Exists(summonableComp.SummonedEntity))
                PredictedDel(summonableComp.SummonedEntity);

            summonableComp.CanSummon = true;
            Dirty(uid, summonableComp);

            // Re-add summon action.
            if (summonableComp.SummonActionEntity is { } action
                && _container.TryGetContainingContainer(uid, out var container))
            {
                _actions.GrantContainedAction(container.Owner, uid, action);
            }

            if (_net.IsServer)
            {
                _popup.PopupEntity(Loc.GetString(summonableComp.SummonRespawnReadyText, ("book", uid)), uid, PopupType.Medium);
                _audio.PlayPvs(summonableComp.SummonSound, uid);
            }

            RemCompDeferred(uid, respawningComponent);
        }
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<BibleComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!TryComp<UseDelayComponent>(ent, out var useDelay) || _delay.IsDelayed((ent, useDelay)))
            return;

        if (args.Target == null || args.Target == args.User || !_mobState.IsAlive(args.Target.Value))
            return;

        var userEnt = Identity.Entity(args.User, EntityManager);
        var targetEnt = Identity.Entity(args.Target.Value, EntityManager);

        // Sizzle user if they are not a Bible user.
        if (!HasComp<BibleUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.SizzleText, ("user", userEnt), ("target", targetEnt), ("bible", ent)), args.User, args.User);

            _audio.PlayPredicted(ent.Comp.SizzleSound, ent, args.User);
            _damageable.TryChangeDamage(args.User, ent.Comp.DamageOnUntrainedUse, true, origin: ent);
            _delay.TryResetDelay((ent, useDelay));

            return;
        }

        // This only has a chance to fail if the target is not wearing anything on their head and is not a familiar.
        if (!_inventory.TryGetSlotEntity(args.Target.Value, "head", out _) && !HasComp<FamiliarComponent>(args.Target.Value))
        {
            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
            if (rand.Prob(ent.Comp.FailChance))
            {
                var othersFailMessage = Loc.GetString(ent.Comp.HealFailOthersText, ("user", userEnt), ("target", targetEnt), ("bible", ent));
                _popup.PopupEntity(othersFailMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);

                var selfFailMessage = Loc.GetString(ent.Comp.HealFailSelfText, ("user", userEnt), ("target", targetEnt), ("bible", ent));
                _popup.PopupEntity(selfFailMessage, args.User, args.User, PopupType.MediumCaution);

                _audio.PlayPredicted(ent.Comp.BibleHitSound, ent, args.User);
                _damageable.TryChangeDamage(args.Target.Value, ent.Comp.DamageOnFail, true, origin: ent);
                _delay.TryResetDelay((ent, useDelay));

                return;
            }
        }

        string othersMessage;
        string selfMessage;

        if (_damageable.TryChangeDamage(args.Target.Value, ent.Comp.Damage, true, origin: ent))
        {
            othersMessage = Loc.GetString(ent.Comp.HealSuccessOthersText, ("user", userEnt), ("target", targetEnt), ("bible", ent));
            selfMessage = Loc.GetString(ent.Comp.HealSuccessSelfText, ("user", userEnt), ("target", targetEnt), ("bible", ent));

            _audio.PlayPredicted(ent.Comp.HealSound, ent, args.User);
            _delay.TryResetDelay((ent, useDelay));

            if (ent.Comp.HealingLightEffect.HasValue)
                PredictedSpawnAtPosition(ent.Comp.HealingLightEffect.Value, Transform(args.Target.Value).Coordinates);
        }
        else
        {
            othersMessage = Loc.GetString(ent.Comp.HealSuccessNoneOthersText, ("user", userEnt), ("target", targetEnt), ("bible", ent));
            selfMessage = Loc.GetString(ent.Comp.HealSuccessNoneSelfText, ("user", userEnt), ("target", targetEnt), ("bible", ent));
        }

        _popup.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);
        _popup.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
    }

    [SubscribeLocalEvent]
    private void AddSummonVerb(Entity<SummonableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !CanSummon(ent, args.User))
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                AttemptSummon(ent, user);
            },
            Text = Loc.GetString(ent.Comp.SummonVerbText),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    [SubscribeLocalEvent]
    private void GetSummonAction(Entity<SummonableComponent> ent, ref GetItemActionsEvent args)
    {
        if (!CanSummon(ent, args.User))
            return;

        args.AddAction(ref ent.Comp.SummonActionEntity, ent.Comp.SummonActionPrototype);
    }

    [SubscribeLocalEvent]
    private void OnSummon(Entity<SummonableComponent> ent, ref SummonActionEvent args)
    {
        AttemptSummon(ent, args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnFamiliarDeath(Entity<FamiliarComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || ent.Comp.Source == null)
            return;

        StartRespawnTimer(ent.Comp.Source.Value);
    }

    [SubscribeLocalEvent]
    private void OnFamiliarRemoved(Entity<FamiliarComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<SummonableComponent>(ent.Comp.Source, out var summonable))
            return;

        // If the entity is no longer a familiar, then there’s no need to keep track of it anymore
        summonable.SummonedEntity = null;
        Dirty(ent.Comp.Source.Value, summonable);

        StartRespawnTimer((ent.Comp.Source.Value, summonable));
    }

    [SubscribeLocalEvent]
    private void OnSummonableRemoved(Entity<SummonableComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<FamiliarComponent>(ent.Comp.SummonedEntity, out var familiar))
            return;

        familiar.Source = null;
        Dirty(ent.Comp.SummonedEntity.Value, familiar);
    }

    /// <summary>
    /// When the familiar spawns, set its source to the bible.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSpawned(Entity<FamiliarComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        var parent = Transform(args.Spawner).ParentUid;
        if (!TryComp<SummonableComponent>(parent, out var summonable))
            return;

        ent.Comp.Source = parent;
        Dirty(ent);

        summonable.SummonedEntity = ent;
        Dirty(parent, summonable);
    }

    /// <summary>
    /// Starts up the respawn timer for the chaplain's familiar.
    /// </summary>
    private void StartRespawnTimer(Entity<SummonableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        EnsureComp<SummonableRespawningComponent>(ent);

        ent.Comp.RespawnTime = _timing.CurTime + ent.Comp.RespawnCooldown;
        Dirty(ent);
    }

    /// <summary>
    /// Checks whether the summonable entity can be summoned by the given user.
    /// </summary>
    private bool CanSummon(Entity<SummonableComponent> ent, EntityUid user)
    {
        return ent.Comp.CanSummon
            && ent.Comp.SummonEntityPrototype.HasValue
            && (!ent.Comp.RequiresBibleUser || HasComp<BibleUserComponent>(user));
    }

    /// <summary>
    /// Attempts to summon a new familiar.
    /// </summary>
    private void AttemptSummon(Entity<SummonableComponent> ent, EntityUid user)
    {
        if (!CanSummon(ent, user))
            return;

        if (!_blocker.CanInteract(user, ent))
            return;

        // Make this familiar the component's summon
        var familiar = PredictedSpawnAtPosition(ent.Comp.SummonEntityPrototype, Transform(user).Coordinates);
        ent.Comp.SummonedEntity = familiar;

        // If this is going to use a ghost role mob spawner, attach it to the bible.
        if (HasComp<GhostRoleMobSpawnerComponent>(familiar))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.SummonRequestedText), user, user, PopupType.Medium);
            _transform.SetParent(familiar, ent);
        }
        else
        {
            EnsureComp<FamiliarComponent>(familiar, out var familiarComponent);
            familiarComponent.Source = ent;
            Dirty(familiar, familiarComponent);
        }

        _actions.RemoveAction(user, ent.Comp.SummonActionEntity);

        ent.Comp.CanSummon = false;
        Dirty(ent);
    }
}
