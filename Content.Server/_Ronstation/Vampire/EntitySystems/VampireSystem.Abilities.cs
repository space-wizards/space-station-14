using Content.Server.Popups;
using Content.Server._Ronstation.Vampire.EntitySystems;
using Content.Shared._Ronstation.Vampire;
using Content.Shared._Ronstation.Vampire.Components;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;


namespace Content.Server._Ronstation.Vampire.EntitySystems;

public sealed partial class VampireSystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public void InitializeAbilities()
    {

        SubscribeLocalEvent<VampireFeedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VampireFeedComponent, VampireFeedActionEvent>(OnFeedAction);
        SubscribeLocalEvent<VampireFeedComponent, VampireFeedDoAfterEvent>(OnFeedDoAfter);
    }

    private void OnMapInit(Entity<VampireFeedComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.VampireFeedActionEntity, ent.Comp.VampireFeedAction);
    }

    private bool IsTargetValid(EntityUid target, Entity<VampireFeedComponent> ent)
    {
        // Targets without blood can't have their blood drank
        if (!TryComp<BloodstreamComponent>(target, out BloodstreamComponent? comp))
        {
            _popupSystem.PopupEntity(Loc.GetString("vampire-feed-attempt-failed-no-bloodstream", ("target", Identity.Entity(target, EntityManager))), ent, ent, PopupType.Medium);
            return false;
        }
        if (SolutionContainer.ResolveSolution(comp.Owner, comp.BloodSolutionName, ref comp.BloodSolution, out var bloodSolution))
        {
            // Can't drink from a target without a beating heart
            if (_mobState.IsDead(target))
            {
                _popupSystem.PopupEntity(Loc.GetString("vampire-feed-attempt-failed-dead", ("target", Identity.Entity(target, EntityManager))), ent, ent, PopupType.Medium);
                return false;
            }
            // Not enough blood = not enough blood flow (to stop people from 'farming')
            if (bloodSolution.Volume < 10)
            {
                _popupSystem.PopupEntity(Loc.GetString("vampire-feed-attempt-failed-low-blood", ("target", Identity.Entity(target, EntityManager))), ent, ent, PopupType.Medium);
                return false;
            }
            return true;
        }
        return false;
    }

    private void OnFeedAction(Entity<VampireFeedComponent> ent, ref VampireFeedActionEvent args)
    {
        // If we already handeled it or our target isn't whitelisted, nope out
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target))
            return;
        // We are now handling the action
        args.Handled = true;
        var target = args.Target;

        // Don't drink yourself, idiot
        if (target == ent.Owner)
            return;

        // Check that our target makes sense
        if (!IsTargetValid(target, ent))
            return;

        // Log it for admins so they can see what's happening
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ent:player} started drinking {target:player}'s blood");

        // I am biting someone/Hey a vampire is biting someone
        _popupSystem.PopupEntity(Loc.GetString("vampire-bite-msg", ("target", Identity.Entity(args.Target, EntityManager))),
            ent.Owner,
            ent.Owner);

        _popupSystem.PopupEntity(
            Loc.GetString("vampire-bite-msg-other", ("user", Identity.Entity(ent.Owner, EntityManager)), ("target", Identity.Entity(args.Target, EntityManager))),
            ent.Owner,
            Filter.PvsExcept(ent.Owner),
            true,
            PopupType.MediumCaution);

        var ev = new TransferDnaEvent { Donor = target, Recipient = ent.Owner };
        RaiseLocalEvent(target, ref ev);


        // Run OnFeedDoafter, passing the necessary args
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.Delay, new VampireFeedDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BlockDuplicate = false,
            DuplicateCondition = DuplicateConditions.None,
        });
    }
    // What happens after the do-after
    private void OnFeedDoAfter(Entity<VampireFeedComponent> ent, ref VampireFeedDoAfterEvent args)
    {
        args.Handled = true;

        // We moved/got interrupted
        if (args.Cancelled)
            return;

        // Somehow they don't have a bloodstream, ergo we can't take anything
        if (!TryComp<BloodstreamComponent>(args.Target, out BloodstreamComponent? bloodstream))
            return;

        // Somehow it can't take damage
        if (!TryComp<DamageableComponent>(args.Target, out var damage))
            return;

        // Check their blood is in their bloodstream
        if (!SolutionContainer.ResolveSolution(bloodstream.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
            return;

        // Target loses some blood
        _bloodstreamSystem.TryModifyBloodLevel(bloodstream.Owner, -ent.Comp.TransferAmount);

        // Target takes some damage, then the slurp sound playsF
        _damageable.TryChangeDamage(args.Target, ent.Comp.DamagePerTick, true, true, damage, args.User);
        _audio.PlayPredicted(ent.Comp.FeedNoise, args.Target.Value, args.User, AudioParams.Default.WithVolume(-2f).WithVariation(0.25f));

        // Vampire gets vitae
        if (TryComp<VampireComponent>(ent.Owner, out VampireComponent? comp))
        {
            ChangeVitaeAmount(ent.Owner, comp.VitaeGainOnDoAfter, comp, false);

            if (LevelUp(ent.Owner, comp))
                _popupSystem.PopupEntity(Loc.GetString("vampire-level-up"),
                    args.User,
                    args.User);
        }

        // Ow my neck
        _popupSystem.PopupEntity(Loc.GetString("vampire-feed-msg"),
            args.User,
            args.User);

        _popupSystem.PopupEntity(Loc.GetString("vampire-feed-msg-others", ("entity", args.User)), args.User, Filter.PvsExcept(args.User), true);

        // Check if we can still feed, if so, repeat
        args.Repeat = IsTargetValid(args.Target.Value, ent);
    }
}