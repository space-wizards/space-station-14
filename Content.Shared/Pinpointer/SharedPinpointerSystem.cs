using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;

namespace Content.Shared.Pinpointer;

public abstract class SharedPinpointerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PinpointerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PinpointerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<PinpointerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PinpointerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PinpointerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    #region Event Subscriptions

    private void OnComponentInit(Entity<PinpointerComponent> ent, ref ComponentInit _)
    {
        if (ent.Comp.AllTargets.Count == 0)
        {
            return;
        }

        // This makes pinpointers start with a target selected, so pinpointers
        // with one target don't require the user to select the singular target
        // before being able to use the pinpointer.
        SetTarget(ent.AsNullable(), ent.Comp.AllTargets[0]);

    }

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

        Dirty(ent);
    }

    /// <summary>
    ///     Adds the target entity to the list of targets if there is space and the pinpointer allows retargeting
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

        // If the pinpointer is at maximum capacity, try to remove the current target before adding the new one.
        // This is so the user is not prevented from adding new targets once the target limit is reached.
        if (ent.Comp.AllTargets.Count >= ent.Comp.TargetLimit && !RemoveTarget(ent.AsNullable(), ent.Comp.Target))
        {
            return;
        }

        AddTarget(ent.AsNullable(), targetListing);
        SetTarget(ent.AsNullable(), targetListing);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):player} set target of {ToPrettyString(ent):pinpointer} to {ToPrettyString(target):target}");
        _popup.PopupClient(Loc.GetString("pinpointer-add-target", ("target", GetName(targetListing))), ent, args.User);
    }

    private void OnExamined(Entity<PinpointerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || ent.Comp.Target is null)
            return;

        args.PushMarkup(Loc.GetString("examine-pinpointer-linked", ("target", GetName(ent.Comp.Target))));
    }

    private void OnGetAltVerbs(Entity<PinpointerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
        {
            return;
        }

        foreach (var target in ent.Comp.AllTargets)
        {
            var user = args.User;
            args.Verbs.Add(new()
            {
                Act = () =>
                {
                    SetTarget(ent.AsNullable(), target);
                    _popup.PopupClient(Loc.GetString("pinpointer-add-target", ("target", GetName(target))), ent, user);
                },
                Text = GetName(target),
                IconEntity = GetNetEntity(args.Target),
            });
        }
    }

    #endregion

    /// <summary>
    ///     Set pinpointers target to track. Updates the pinpointer's PinpointerTarget. Use this to logically update
    ///     what the pinpointer should be pointing to, i.e. when the pinpointer needs to point to a new kind of target.
    /// </summary>
    protected void SetTarget(Entity<PinpointerComponent?> ent, PinpointerTarget? target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var pinpointer = ent.Comp;

        if (pinpointer.Target == target)
            return;

        pinpointer.Target = target;
        Dirty(ent);
        UpdateTargetEntity(ent);
    }

    /// <summary>
    ///     Attempts to add a target to the list of targets of a pinpointer.
    /// </summary>
    [PublicAPI]
    public bool AddTarget(Entity<PinpointerComponent?> ent, PinpointerTarget? target)
    {
        if (!Resolve(ent, ref ent.Comp))
        {
            return false;
        }

        if (target is null)
        {
            return false;
        }

        if (ent.Comp.AllTargets.Count >= ent.Comp.TargetLimit)
        {
            return false;
        }

        ent.Comp.AllTargets.Add(target);
        Dirty(ent);

        return true;
    }

    /// <summary>
    ///     Attempts to remove a target from the list of targets of a pinpointer.
    /// </summary>
    [PublicAPI]
    public bool RemoveTarget(Entity<PinpointerComponent?> ent, PinpointerTarget? target)
    {
        if (!Resolve(ent, ref ent.Comp))
        {
            return false;
        }

        if (target is null)
        {
            return false;
        }

        if (ent.Comp.AllTargets.Count == 0)
        {
            return false;
        }

        if (ent.Comp.Target == target)
        {
            ent.Comp.Target = null;
            UpdateTargetEntity(ent);
        }
        var result = ent.Comp.AllTargets.Remove(target);
        Dirty(ent);

        return result;
    }

    /// <summary>
    ///     Set pinpointer's entity target to track. Updates the specific entity the pinpointer is pointing at. Use this
    ///     to refresh the exact entity the pinpointer is pointing to, i.e. when you turn the pinpointer on or when
    ///     you swap the logical target.
    /// </summary>
    /// <param name="ent"></param>
    protected virtual void UpdateTargetEntity(Entity<PinpointerComponent?> ent)
    {

    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected virtual void UpdateDirectionToTarget(Entity<PinpointerComponent?> ent)
    {

    }

    protected string GetName(PinpointerTarget target)
    {
        return target.Name ?? Loc.GetString("pinpointer-unknown-target");
    }

    /// <summary>
    ///     Manually set distance from pinpointer to target
    /// </summary>
    protected void SetDistance(Entity<PinpointerComponent?> ent, Distance distance)
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
    protected bool TrySetArrowAngle(Entity<PinpointerComponent?> ent, Angle arrowAngle)
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
    ///     Activate/deactivate pinpointer screen.
    /// </summary>
    [PublicAPI]
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
    ///     Toggle pinpointer screen.
    /// </summary>
    /// <returns>True if pinpointer was toggled successfully.</returns>
    [PublicAPI]
    public virtual bool TogglePinpointer(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var isActive = !ent.Comp.IsActive;
        SetActive(ent, isActive);
        return true;
    }
}
