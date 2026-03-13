using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Verbs;

namespace Content.Shared.Pinpointer;

public abstract class SharedPinpointerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PinpointerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<PinpointerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PinpointerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PinpointerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    #region Event Subscriptions

    private void OnEmagged(Entity<PinpointerComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        if (ent.Comp.CanRetarget)
            return;

        args.Handled = true;
        ent.Comp.CanRetarget = true;
    }

    /// <summary>
    ///     Set the target if capable
    /// </summary>
    private void OnAfterInteract(Entity<PinpointerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        // Don't retarget if you can't or the pinpointer is currently on
        if (!ent.Comp.CanRetarget || ent.Comp.IsActive)
            return;

        args.Handled = true;

        var targetListing = new PinpointerEntityUidTarget
        {
            Name = Identity.Name(target, EntityManager),
            Target = target,
        };

        AddTarget(ent.AsNullable(), targetListing);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):player} set target of {ToPrettyString(ent):pinpointer} to {ToPrettyString(target):target}");
    }

    private void OnExamined(Entity<PinpointerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || ent.Comp.Target is null)
            return;

        // TODO: Move to loc string
        args.PushMarkup(Loc.GetString("examine-pinpointer-linked", ("target", GetName(ent.Comp.Target))));
    }

    private void OnGetAltVerbs(Entity<PinpointerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
        {
            return;
        }

        foreach (var target in ent.Comp.AllTargets)
        {
            args.Verbs.Add(new()
            {
                Act = () =>
                {
                    SetTarget(ent.AsNullable(), target);
                },
                Text = GetName(target),
            });
        }
    }

    #endregion

    /// <summary>
    ///     Set pinpointers target to track. Updates the pinpointer's PinpointerTarget. Use this to logically update
    ///     what the pinpointer should be pointing to, i.e. when the pinpointer needs to point to a new kind of target.
    /// </summary>
    public void SetTarget(Entity<PinpointerComponent?> ent, PinpointerTarget? target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var pinpointer = ent.Comp;

        if (pinpointer.Target == target)
            return;

        pinpointer.Target = target;
        UpdateTargetEntity(ent);
    }

    /// <summary>
    ///
    /// </summary>
    public void AddTarget(Entity<PinpointerComponent?> ent, PinpointerTarget? target)
    {
        if (!Resolve(ent, ref ent.Comp))
        {
            return;
        }

        if (target is null)
        {
            return;
        }

        if (ent.Comp.AllTargets.Count >= ent.Comp.TargetLimit)
        {
            return;
        }

        ent.Comp.AllTargets.Add(target);
    }

    /// <summary>
    ///     Set pinpointer's entity target to track. Updates the specific entity the pinpointer is pointing at. Use this
    ///     to refresh the exact entity the pinpointer is pointing to, i.e. when you turn the pinpointer on or when
    ///     you swap the logical target.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="target"></param>
    protected virtual void UpdateTargetEntity(Entity<PinpointerComponent?> ent)
    {

    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected virtual void UpdateDirectionToTarget(Entity<PinpointerComponent?> ent)
    {

    }

    public string GetName(PinpointerTarget target)
    {
        // TODO update with a loc string
        return target.Name ?? "Unknown";
    }

    /// <summary>
    ///     Manually set distance from pinpointer to target
    /// </summary>
    public void SetDistance(Entity<PinpointerComponent?> ent, Distance distance)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (distance == ent.Comp.DistanceToTarget)
            return;

        ent.Comp.DistanceToTarget = distance;
        Dirty(ent);
    }

    /// <summary>
    ///     Try to manually set pinpointer arrow direction.
    ///     If difference between current angle and new angle is smaller than
    ///     pinpointer precision, new value will be ignored and it will return false.
    /// </summary>
    public bool TrySetArrowAngle(Entity<PinpointerComponent?> ent, Angle arrowAngle)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.ArrowAngle.EqualsApprox(arrowAngle, ent.Comp.Precision))
            return false;

        ent.Comp.ArrowAngle = arrowAngle;
        Dirty(ent);

        return true;
    }

    /// <summary>
    ///     Activate/deactivate pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    public void SetActive(Entity<PinpointerComponent?> ent, bool isActive)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (isActive == ent.Comp.IsActive)
            return;

        ent.Comp.IsActive = isActive;
        Dirty(ent);
    }

    /// <summary>
    ///     Toggle Pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    /// <returns>True if pinpointer was activated, false otherwise</returns>
    public virtual bool TogglePinpointer(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var isActive = !ent.Comp.IsActive;
        SetActive(ent, isActive);
        return isActive;
    }
}
