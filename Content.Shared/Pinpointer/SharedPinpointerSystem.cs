using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Network;

namespace Content.Shared.Pinpointer;

public abstract class SharedPinpointerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

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
            SetTarget(uid, args.Target, component, args.User);
        }
        else
        {
            var pinpointerScanEvent = new GotPinpointerScannedEvent(uid, component, args.User);
            RaiseLocalEvent(args.Target.Value, ref pinpointerScanEvent);

            if (!pinpointerScanEvent.Handled)
                return;
        }

        StoreTarget(component.Target, uid, component, args.User);
    }

    /// <summary>
    ///     Set pinpointers target to track
    /// </summary>
    public virtual void SetTarget(EntityUid uid, EntityUid? target, PinpointerComponent pinpointer, EntityUid? user = null, bool toggleOn = false)
    {

        if (pinpointer.Target == target)
            return;

        pinpointer.Target = target;

        //Searches for the name of the tracked entity
        if (pinpointer.Target != null)
        {
            pinpointer.TargetName = Identity.Name(pinpointer.Target.Value, EntityManager);
        }

        //Turns on the pinpointer
        if (!pinpointer.IsActive && toggleOn)
        {
            TogglePinpointer(uid, pinpointer);
        }

        Dirty(uid, pinpointer);

        if (user != null && pinpointer.Target != null)
        {
            if (pinpointer.TargetName != null && toggleOn)
            {
                _popup.PopupEntity(Loc.GetString("targeting-pinpointer-succeeded",
                        ("target", pinpointer.TargetName)),
                    user.Value,
                    user.Value);
            }

        }
        else
        {
            TogglePinpointer(uid, pinpointer);
        }

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
        Dirty(uid, pinpointer);
    }

    /// <summary>
    /// Stores the located target on the pinpointer if it's not already stored.
    /// Adds the Trackable component to the target so it can keep track of all the pinpointers tracking it.
    /// </summary>
    public void StoreTarget(EntityUid? target, EntityUid pinpointer, PinpointerComponent component, EntityUid user)
    {
        if (target == null)
        {
            _popup.PopupEntity(Loc.GetString("targeting-pinpointer-failed"), user, user);
            return;
        }

        if (component.StoredTargets.Count >= component.MaxTargets)
        {
            _popup.PopupClient(Loc.GetString("target-pinpointer-full"),user,user);
            return;
        }

        if (component.StoredTargets.Contains(target.Value))
            return;

        component.StoredTargets.Add(target.Value);
        EnsureComp<TrackableComponent>(target.Value, out var trackable);
        trackable.TrackedBy.Add(pinpointer);

        Dirty(pinpointer, component);

        if (_net.IsServer && !component.IsActive)
        {
            _popup.PopupEntity(Loc.GetString("target-pinpointer-stored", ("target", target)), user, user);
        }

    }

    /// <summary>
    ///     Removes a target from the target list.
    /// </summary>
    public void RemoveTarget(EntityUid uid, PinpointerComponent component, EntityUid tracker)
    {
        component.StoredTargets.Remove(uid);
        Dirty(tracker, component);
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
        Dirty(uid,pinpointer);

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
        Dirty(uid, pinpointer);
    }


    /// <summary>
    ///     Toggle Pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    /// <returns>True if pinpointer was activated, false otherwise</returns>
    public virtual bool TogglePinpointer(EntityUid uid, PinpointerComponent? pinpointer = null)
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

/// <summary>
///     Gets raised when the pinpointer is used on another entity.
/// </summary>
[ByRefEvent]
public record struct GotPinpointerScannedEvent(EntityUid Pinpointer, PinpointerComponent Component, EntityUid User,  bool Handled = false);
