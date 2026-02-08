using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;

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
    }

    /// <summary>
    ///     Set the target if capable
    /// </summary>
    private void OnAfterInteract(Entity<PinpointerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        if (!ent.Comp.CanRetarget || ent.Comp.IsActive)
            return;

        // TODO add doafter once the freeze is lifted
        args.Handled = true;
        ent.Comp.Target = args.Target;
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):player} set target of {ToPrettyString(ent):pinpointer} to {ToPrettyString(ent.Comp.Target.Value):target}");
        if (ent.Comp.UpdateTargetName)
            ent.Comp.TargetName = ent.Comp.Target == null ? null : Identity.Name(ent.Comp.Target.Value, EntityManager);
    }

    /// <summary>
    ///     Set pinpointers target to track
    /// </summary>
    public virtual void SetTarget(Entity<PinpointerComponent> ent, EntityUid? target)
    {
        var pinpointer = ent.Comp;

        if (pinpointer.Target == target)
            return;

        pinpointer.Target = target;
        if (pinpointer.UpdateTargetName)
            pinpointer.TargetName = target == null ? null : Identity.Name(target.Value, EntityManager);
        if (pinpointer.IsActive)
            UpdateDirectionToTarget(ent);
    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected virtual void UpdateDirectionToTarget(Entity<PinpointerComponent> ent)
    {

    }

    private void OnExamined(Entity<PinpointerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || ent.Comp.TargetName == null)
            return;

        args.PushMarkup(Loc.GetString("examine-pinpointer-linked", ("target", ent.Comp.TargetName)));
    }

    /// <summary>
    ///     Manually set distance from pinpointer to target
    /// </summary>
    public void SetDistance(Entity<PinpointerComponent> ent, Distance distance)
    {
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
    public bool TrySetArrowAngle(Entity<PinpointerComponent> ent, Angle arrowAngle)
    {
        if (ent.Comp.ArrowAngle.EqualsApprox(arrowAngle, ent.Comp.Precision))
            return false;

        ent.Comp.ArrowAngle = arrowAngle;
        Dirty(ent);

        return true;
    }

    /// <summary>
    ///     Activate/deactivate pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    public void SetActive(Entity<PinpointerComponent> ent, bool isActive)
    {
        if (isActive == ent.Comp.IsActive)
            return;

        ent.Comp.IsActive = isActive;
        Dirty(ent);
    }


    /// <summary>
    ///     Toggle Pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    /// <returns>True if pinpointer was activated, false otherwise</returns>
    public virtual bool TogglePinpointer(Entity<PinpointerComponent> ent)
    {
        var isActive = !ent.Comp.IsActive;
        SetActive(ent, isActive);
        return isActive;
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
    }
}
