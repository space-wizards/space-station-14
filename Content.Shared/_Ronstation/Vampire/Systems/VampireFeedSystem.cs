using Content.Shared._Ronstation.Vampire.Components;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._Ronstation.Vampire.Systems;

public sealed class VampireFeedSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<VampireFeedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VampireFeedComponent, VampireFeedActionEvent>(OnFeedAction);
        SubscribeLocalEvent<VampireFeedComponent, VampireFeedDoAfterEvent>(OnFeedDoAfter);
        SubscribeLocalEvent<VampireFeedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<VampireFeedComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.VampireFeedActionEntity, ent.Comp.VampireFeedAction);
    }

    private void OnShutdown(Entity<VampireFeedComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.VampireFeedActionEntity != null)
        {
            _actionsSystem.RemoveAction(ent.Owner, ent.Comp.VampireFeedActionEntity);
        }
    }

    private bool IsTargetValid(EntityUid target, Entity<VampireFeedComponent> ent)
    {
        if (!TryComp<BloodstreamComponent>(target, out BloodstreamComponent? comp))
            return false;

        if (SolutionContainer.ResolveSolution(comp.Owner, comp.BloodSolutionName, ref comp.BloodSolution, out var bloodSolution))
        {
            if (_mobState.IsDead(target))
            {
                _popupSystem.PopupClient(Loc.GetString("vampire-feed-attempt-failed-dead"), ent, ent, PopupType.Medium);
                return false;
            }
            if (bloodSolution.Volume < 10)
            {
                _popupSystem.PopupClient(Loc.GetString("vampire-feed-attempt-failed-low-blood"), ent, ent, PopupType.Medium);
                return false;
            }
            return true;
        }
        return false;
    }

    private void OnFeedAction(Entity<VampireFeedComponent> ent, ref VampireFeedActionEvent args)
    {
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target))
            return;

        args.Handled = true;
        var target = args.Target;

        if (target == ent.Owner)
            return;

        if (!IsTargetValid(target, ent))
            return;
        // _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ent:player} started drinking {target:player}'s blood");

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.Delay, new VampireFeedDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });
    }

    private void OnFeedDoAfter(Entity<VampireFeedComponent> ent, ref VampireFeedDoAfterEvent args)
    {
        args.Handled = true;

        // if (!EntityManager.EntityExists(ent.Comp.CurrentDevourSound))
        //     _audio.Stop(ent.Comp.CurrentDevourSound!);

        if (args.Cancelled)
            return;

        if (!TryComp<BloodstreamComponent>(args.Target, out BloodstreamComponent? bloodstream))
            return;

        if (!TryComp<DamageableComponent>(args.Target, out var damage))
            return;

        if (!SolutionContainer.ResolveSolution(bloodstream.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
            return;

        bloodSolution.Volume = bloodSolution.Volume - 10;
        if (bloodSolution.Volume < 0)
            bloodSolution.Volume = 0;

        _damageable.TryChangeDamage(args.Target, ent.Comp.DamagePerTick, true, true, damage, args.User);
        _audio.PlayPredicted(ent.Comp.FeedNoise, args.Target.Value, args.User, AudioParams.Default.WithVolume(-2f).WithVariation(0.25f));

        if (TryComp<VampireComponent>(ent.Owner, out VampireComponent? comp))
        {
            comp.VitaeRegenCap = comp.VitaeRegenCap + comp.VitaeCapUpgradeAmount;
        }
        args.Repeat = IsTargetValid(args.Target.Value, ent);
    }
}