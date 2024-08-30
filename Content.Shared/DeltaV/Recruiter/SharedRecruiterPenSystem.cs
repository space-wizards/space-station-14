using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DeltaV.Paper;
using Content.Shared.DeltaV.Roles;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared.DeltaV.Recruiter;

/// <summary>
/// Handles finger pricking and signing for the recruiter pen.
/// </summary>
public abstract class SharedRecruiterPenSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private EntityQuery<MindShieldComponent> _shieldQuery;

    public override void Initialize()
    {
        base.Initialize();

        _shieldQuery = GetEntityQuery<MindShieldComponent>();

        SubscribeLocalEvent<RecruiterPenComponent, HandSelectedEvent>(OnHandSelected);
        SubscribeLocalEvent<RecruiterPenComponent, UseInHandEvent>(OnPrick);
        SubscribeLocalEvent<RecruiterPenComponent, SignAttemptEvent>(OnSignAttempt);
    }

    private void OnHandSelected(Entity<RecruiterPenComponent> ent, ref HandSelectedEvent args)
    {
        var (uid, comp) = ent;
        if (comp.RecruiterMind != null)
            return;

        // mind isnt networked properly so the popup is only done on server
        var user = args.User;
        if (!Mind.TryGetMind(user, out var mind, out _))
            return;

        if (!HasComp<RecruiterRoleComponent>(mind))
            return;

        Popup.PopupEntity(Loc.GetString("recruiter-pen-bound", ("pen", uid)), user, user);

        comp.RecruiterMind = mind;
        comp.Bound = true;
        Dirty(uid, comp);
    }

    private void OnPrick(Entity<RecruiterPenComponent> ent, ref UseInHandEvent args)
    {
        var (uid, comp) = ent;
        if (args.Handled || !comp.Bound)
            return;

        if (!_solution.TryGetSolution(uid, comp.Solution, out var dest, true))
            return;

        args.Handled = true;

        // sec would never defect, silly recruiter
        var user = args.User;
        if (CheckBlacklist(ent, user, "prick"))
            return;

        DrawBlood(ent, dest.Value, user);
    }

    private void OnSignAttempt(Entity<RecruiterPenComponent> ent, ref SignAttemptEvent args)
    {
        var (uid, comp) = ent;
        if (args.Cancelled)
            return;

        args.Cancelled = true;

        if (!_solution.TryGetSolution(uid, comp.Solution, out var blood, true))
            return;

        var user = args.User;
        if (!comp.Bound)
        {
            Popup.PopupEntity(Loc.GetString("recruiter-pen-locked", ("pen", uid)), user, user);
            return;
        }

        if (CheckBlacklist(ent, user, "sign"))
            return;

        if (blood.Value.Comp.Solution.AvailableVolume > 0)
        {
            Popup.PopupEntity(Loc.GetString("recruiter-pen-empty", ("pen", uid)), user, user);
            return;
        }

        _solution.RemoveAllSolution(blood.Value);
        Recruit(ent, user);

        // all checks passed, let it be signed!
        args.Cancelled = false;
    }

    private bool CheckBlacklist(Entity<RecruiterPenComponent> ent, EntityUid user, string action)
    {
        if (!Mind.TryGetMind(user, out var mind, out _))
            return false; // mindless nt drone...

        var (uid, comp) = ent;
        if (_whitelist.IsBlacklistPass(comp.Blacklist, user) || _whitelist.IsBlacklistPass(comp.MindBlacklist, mind))
        {
            Popup.PopupPredicted(Loc.GetString($"recruiter-pen-{action}-forbidden", ("pen", uid)), user, user);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to draw blood from the user into the pen.
    /// Returns true if there was a popup.
    /// </summary>
    protected virtual void DrawBlood(EntityUid uid, Entity<SolutionComponent> dest, EntityUid user)
    {
    }

    /// <summary>
    /// Handle greentext if the user is signing something for the first time.
    /// </summary>
    protected virtual void Recruit(Entity<RecruiterPenComponent> ent, EntityUid user)
    {
    }
}
