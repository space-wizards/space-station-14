using Content.Shared.Mind;
using Content.Shared.Actions;
using Content.Shared.Administration.Systems;
using Content.Shared.Changeling.Components;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Changeling.Systems;

public abstract partial class SharedChangelingLastResortSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingLastResortAbilityComponent, ChangelingLastResortActionEvent>(OnLastResortAction);
        SubscribeLocalEvent<ChangelingSlugComponent, MapInitEvent>(OnTakeOverMapInit);
        SubscribeLocalEvent<ChangelingSlugComponent, ComponentShutdown>(OnTakeOverShutdown);
        SubscribeLocalEvent<ChangelingSlugComponent, ChangelingTakeOverCorpseActionEvent>(OnTakeOverCorpseAction);
        SubscribeLocalEvent<ChangelingSlugComponent, ChangelingTakeOverCorpseDoAfterEvent>(OnTakeOverCorpseDoAfter);
    }

    private void OnTakeOverMapInit(Entity<ChangelingSlugComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnTakeOverShutdown(Entity<ChangelingSlugComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnLastResortAction(Entity<ChangelingLastResortAbilityComponent> ent,
        ref ChangelingLastResortActionEvent args)
    {
        if (args.Handled || !_mind.TryGetMind(args.Performer, out var mindId, out var mind))
            return;

        args.Handled = true;

        _audio.PlayPredicted(ent.Comp.Sound, args.Performer, args.Performer);

        if (!_net.IsServer)
            return; // Transfer Mind is unpredictable.

        var slug = Spawn(ent.Comp.SlugPrototype, Transform(args.Performer).Coordinates);
        _mind.TransferTo(mindId, slug, mind: mind);
        _gibbing.Gib(args.Performer);
    }

    private void OnTakeOverCorpseAction(Entity<ChangelingSlugComponent> ent,
        ref ChangelingTakeOverCorpseActionEvent args)
    {
        if (args.Handled || !CanTakeOver(ent.Owner, args.Target))
            return;

        args.Handled = true;

        _audio.PlayPredicted(ent.Comp.Sound, ent.Owner, ent.Owner);
        _popup.PopupPredicted(Loc.GetString("changeling-takeover-start-others", ("user", ent.Owner)),
            ent.Owner,
            ent.Owner,
            PopupType.MediumCaution);

        var doAfter = new DoAfterArgs(EntityManager,
            ent.Owner,
            ent.Comp.TakeOverDuration,
            new ChangelingTakeOverCorpseDoAfterEvent(),
            ent,
            target: args.Target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.None,
            RequireCanInteract = false,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnTakeOverCorpseDoAfter(Entity<ChangelingSlugComponent> ent,
        ref ChangelingTakeOverCorpseDoAfterEvent args)
    {
        args.Handled = true;

        if (args.Cancelled || args.Target is not { } target || !CanTakeOver(args.User, target))
            return;

        if (!_mind.TryGetMind(args.User, out var mindId, out var mind))
            return;

        // TODO: delete this after adding the stasis.
        _rejuvenate.PerformRejuvenate(target);
        _mind.TransferTo(mindId, target, mind: mind);
        TakeOverCorpse(target, mind);
        PredictedQueueDel(args.User);

        _popup.PopupEntity(Loc.GetString("changeling-takeover-success-self"), target, target, PopupType.Large);
    }

    protected virtual void TakeOverCorpse(EntityUid target, MindComponent mind)
    {
    }

    private bool CanTakeOver(EntityUid user, EntityUid target)
    {
        if (!HasComp<HumanoidProfileComponent>(target))
            return false;

        if (_mobState.IsDead(target))
            return true;

        _popup.PopupClient(Loc.GetString("changeling-takeover-not-dead"), user, user);
        return false;
    }
}
