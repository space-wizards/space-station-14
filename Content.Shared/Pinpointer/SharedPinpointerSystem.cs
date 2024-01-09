using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nuke;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Network;

namespace Content.Shared.Pinpointer;

public abstract class SharedPinpointerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

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
    private void OnAfterInteract(EntityUid uid, PinpointerComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        // TODO add doafter once the freeze is lifted
        args.Handled = true;

        if (component.CanRetarget && !component.IsActive || _tagSystem.HasTag(args.Target.Value, "PinpointerScannable"))
        {
            component.Target = args.Target;
        }
        else
        {
            var pinpointerScanEvent = new GotPinpointerScannedEvent(uid);
            RaiseLocalEvent(args.Target.Value, ref pinpointerScanEvent);

            if (component.Target == null)
                return;
        }

        if(component.StoredTargets.Contains(component.Target.Value))
            return;

        if (component.StoredTargets.Count >= component.MaxTargets)
        {
            if (_net.IsServer)
            {
                _popup.PopupEntity(Loc.GetString("target-pinpointer-full"),args.User,args.User);
            }
            return;
        }

        StoreTarget(component.Target.Value, uid, component);

        if (_net.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("target-pinpointer-stored", ("target", component.Target.Value)), args.User, args.User);
        }

        if (component.UpdateTargetName)
            component.TargetName = component.Target == null ? null : Identity.Name(component.Target.Value, EntityManager);
    }

    /// <summary>
    ///     Set pinpointers target to track
    /// </summary>
    public void SetTarget(EntityUid uid, EntityUid? target, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;

        if (pinpointer.Target == target)
            return;

        pinpointer.Target = target;
        if (pinpointer.UpdateTargetName)
            pinpointer.TargetName = target == null ? null : Identity.Name(target.Value, EntityManager);
        if (pinpointer.IsActive)
            UpdateDirectionToTarget(uid, pinpointer);
    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected virtual void UpdateDirectionToTarget(EntityUid uid, PinpointerComponent? pinpointer = null)
    {

    }

    private void OnExamined(EntityUid uid, PinpointerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || component.TargetName == null)
            return;

        args.PushMarkup(Loc.GetString("examine-pinpointer-linked", ("target", component.TargetName)));
    }

    /// <summary>
    ///     Manually set distance from pinpointer to target
    /// </summary>
    public void SetDistance(EntityUid uid, Distance distance, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;

        if (distance == pinpointer.DistanceToTarget)
            return;

        pinpointer.DistanceToTarget = distance;
        Dirty(pinpointer);
    }

    /// <summary>
    /// Stores the located target on the pinpointer if it's not already stored.
    /// Adds the Trackable component to the target so it can keep track of all the pinpointers tracking it.
    /// </summary>
    public void StoreTarget(EntityUid? target, EntityUid pinpointer, PinpointerComponent component, EntityUid? user = null)
    {
        if (target == null || component.StoredTargets.Contains(target.Value))
            return;

        component.StoredTargets.Add(target.Value);
        EnsureComp<TrackableComponent>(target.Value, out var trackable);
        trackable.TrackedBy.Add(pinpointer);
    }

    /// <summary>
    ///     Try to manually set pinpointer arrow direction.
    ///     If difference between current angle and new angle is smaller than
    ///     pinpointer precision, new value will be ignored and it will return false.
    /// </summary>
    public bool TrySetArrowAngle(EntityUid uid, Angle arrowAngle, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return false;

        if (pinpointer.ArrowAngle.EqualsApprox(arrowAngle, pinpointer.Precision))
            return false;

        pinpointer.ArrowAngle = arrowAngle;
        Dirty(pinpointer);

        return true;
    }

    /// <summary>
    ///     Activate/deactivate pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    public void SetActive(EntityUid uid, bool isActive, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;
        if (isActive == pinpointer.IsActive)
            return;

        pinpointer.IsActive = isActive;
        Dirty(pinpointer);
    }


    /// <summary>
    ///     Toggle Pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    /// <returns>True if pinpointer was activated, false otherwise</returns>
    public bool TogglePinpointer(EntityUid uid, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return false;

        var isActive = !pinpointer.IsActive;
        SetActive(uid, isActive, pinpointer);
        return isActive;
    }

    private void OnEmagged(EntityUid uid, PinpointerComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
        component.CanRetarget = true;
    }
}

[ByRefEvent]
public record struct GotPinpointerScannedEvent(EntityUid Pinpointer);
