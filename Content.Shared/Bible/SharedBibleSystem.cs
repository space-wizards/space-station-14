using Content.Shared.ActionBlocker;
using Content.Shared.Bible.Components;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared.Bible;

/// <summary>
/// Shared bible system basically for GetVerbsEvent predictions.
/// </summary>
public abstract class SharedBibleSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem PopupSys = default!;
    [Dependency] protected readonly SharedAudioSystem AudioSys = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly InventorySystem _invSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SummonableComponent, GetVerbsEvent<AlternativeVerb>>(AddSummonVerb);
        SubscribeLocalEvent<BibleComponent, AfterInteractEvent>(OnAfterInteract);
    }

    /// <summary>
    /// Handles verb display for summoning, so the verb is predicted client-side.
    /// </summary>
    private void AddSummonVerb(EntityUid uid, SummonableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || component.AlreadySummoned || component.SpecialItemPrototype == null)
            return;

        // Double-check here and in <see cref="AttemptSummon"/> so players who aren't allowed to summon
        // (e.g., with RequiresBibleUser and without <see cref="BibleUserComponent"/> component) never see the verb.
        if (!CheckSummonable((uid, component), args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                AttemptSummon((uid, component), args.User);
            },
            Text = Loc.GetString("bible-summon-verb"),
            Priority = 2
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Determines whether <paramref name="user"/> is currently allowed to summon <paramref name="ent"/>.
    /// </summary>
    /// <remarks>
    /// Validates component state and requirements (including <see cref="BibleUserComponent"/> if needed),
    /// ensures neither entity is <see cref="EntityLifeStage.Deleted"/> or <see cref="EntityLifeStage.Terminating"/>, and checks standard interaction blockers.
    /// </remarks>
    /// <returns><c>true</c> if summoning is permitted at this time; otherwise, <c>false</c>.</returns>
    private bool CheckSummonable(Entity<SummonableComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        if (component.AlreadySummoned || component.SpecialItemPrototype == null)
            return false;
        if (component.RequiresBibleUser && !HasComp<BibleUserComponent>(user))
            return false;
        if (component.Deleted || TerminatingOrDeleted(uid) || TerminatingOrDeleted(user))
            return false;
        if (!_blocker.CanInteract(user, uid))
            return false;

        return true;
    }

    /// <summary>
    /// Try to summon with checks.
    /// </summary>
    protected void AttemptSummon(Entity<SummonableComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        // TODO : Since this is predictable, maybe show a popup with feedback to the player/client.
        // To handle every possible reason why players cannot summon.
        if (!CheckSummonable(ent, user))
        {
            PopupSys.PopupClient(Loc.GetString("bible-summon-verb-fail"), user, PopupType.MediumCaution);
            return;
        }

        Summon(ent, user, Transform(ent));
    }

    /// <summary>
    /// Internal server's side entity summoning.
    /// </summary>
    /// <remarks>
    /// Only activated on Shared side when <see cref="SharedBibleSystem.CheckSummonable"/> passed.
    /// </remarks>
    protected virtual void Summon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent position)
    {
        // Server-side logic.
    }

    private void OnAfterInteract(Entity<BibleComponent> ent, ref AfterInteractEvent args)
    {
        var component = ent.Comp;
        var uid = ent.Owner;

        if (!args.CanReach)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _delay.IsDelayed((uid, useDelay)))
            return;

        if (args.Target == null || args.Target == args.User || !_mobStateSystem.IsAlive(args.Target.Value))
        {
            return;
        }

        if (!HasComp<BibleUserComponent>(args.User))
        {
            PopupSys.PopupEntity(Loc.GetString("bible-sizzle"), args.User, args.User);

            AudioSys.PlayPvs(component.SizzleSoundPath, args.User);
            _damageableSystem.TryChangeDamage(args.User, component.DamageOnUntrainedUse, true, origin: uid);
            _delay.TryResetDelay((uid, useDelay));

            return;
        }

        // This only has a chance to fail if the target is not wearing anything on their head and is not a familiar.
        if (!_invSystem.TryGetSlotEntity(args.Target.Value, "head", out var _) && !HasComp<FamiliarComponent>(args.Target.Value))
        {
            if (_random.Prob(component.FailChance))
            {
                var othersFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                PopupSys.PopupEntity(othersFailMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);

                var selfFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-self", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
                PopupSys.PopupEntity(selfFailMessage, args.User, args.User, PopupType.MediumCaution);

                AudioSys.PlayPvs(component.BibleHitSound, args.User);
                _damageableSystem.TryChangeDamage(args.Target.Value, component.DamageOnFail, true, origin: uid);
                _delay.TryResetDelay((uid, useDelay));
                return;
            }
        }

        var damage = _damageableSystem.TryChangeDamage(args.Target.Value, component.Damage, true, origin: uid);

        if (damage == null || damage.Empty)
        {
            var othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
            PopupSys.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);

            var selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-self", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
            PopupSys.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
        }
        else
        {
            var othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
            PopupSys.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);

            var selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-self", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("bible", uid));
            PopupSys.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
            AudioSys.PlayPvs(component.HealSoundPath, args.User);
            _delay.TryResetDelay((uid, useDelay));
        }
    }
}
