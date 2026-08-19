using Content.Server.Antag;
using Content.Shared.Administration.Systems;
using Content.Shared.Antag;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingLastResortSystem : SharedChangelingLastResortSystem
{
    private static readonly ProtoId<AntagSpecifierPrototype> ChangelingAntag = "Changeling";

    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;

    [SubscribeLocalEvent]
    private void OnTakeOverCorpseAction(Entity<ChangelingSlugComponent> ent,
        ref ChangelingTakeOverCorpseActionEvent args)
    {
        // CanTakeOver checks for the existence of ChangelingIdentityComponent on the target.
        // As such it cannot be predicted as that component is not networked to sessions other than the owner
        // Unless we do fucky networking magic
        if (args.Handled || !CanTakeOver(ent.Owner, args.Target))
            return;

        args.Handled = true;

        Audio.PlayPvs(ent.Comp.Sound, ent.Owner);
        _popup.PopupEntity(Loc.GetString("changeling-takeover-start-others", ("user", Identity.Entity(ent.Owner, EntityManager))),
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

    /// <summary>
    /// Checks whether a changeling slug can take over the <paramref name="target"/> body.
    /// </summary>
    private bool CanTakeOver(EntityUid user, EntityUid target, bool showPopups = true)
    {
        if (!HasComp<HumanoidProfileComponent>(target))
            return false;

        if (HasComp<ChangelingIdentityComponent>(target))
        {
            if (showPopups)
                _popup.PopupEntity(Loc.GetString("changeling-takeover-is-changeling"), user);
            return false;
        }

        if (_mobState.IsDead(target))
            return true;

        if (showPopups)
            _popup.PopupEntity(Loc.GetString("changeling-takeover-not-dead"), user);
        return false;
    }

    [SubscribeLocalEvent]
    private void OnTakeOverCorpseDoAfter(Entity<ChangelingSlugComponent> ent,
        ref ChangelingTakeOverCorpseDoAfterEvent args)
    {
        args.Handled = true;

        if (args.Cancelled || args.Target is not { } target || !CanTakeOver(args.User, target, showPopups: false))
            return;

        if (!_mind.TryGetMind(args.User, out var mindId, out var mind))
            return;

        TakeOverCorpse(args.User, target, mindId, mind);

        _popup.PopupEntity(Loc.GetString("changeling-takeover-success-self"), target, target, PopupType.Large);
    }

    private void TakeOverCorpse(EntityUid user, EntityUid target, EntityUid mindId, MindComponent mind)
    {
        // TODO: delete this after adding the stasis.
        _rejuvenate.PerformRejuvenate(target);
        _mind.TransferTo(mindId, target, mind: mind);

        _antag.AssignAntagComponents(target, ChangelingAntag);

        QueueDel(user);
    }
}
