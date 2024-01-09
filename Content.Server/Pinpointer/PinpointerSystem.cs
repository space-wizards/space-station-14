using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using System.Linq;
using System.Numerics;
using Content.Server.Popups;
using Content.Server.Shuttles.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Tag;
using Content.Shared.Verbs;
namespace Content.Server.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<PinpointerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<PinpointerComponent, GetVerbsEvent<Verb>>(OnPinpointerVerb);
    }

    public bool TogglePinpointer(EntityUid uid, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return false;

        var isActive = !pinpointer.IsActive;
        SetActive(uid, isActive, pinpointer);
        UpdateAppearance(uid, pinpointer);
        return isActive;
    }

    private void UpdateAppearance(EntityUid uid, PinpointerComponent pinpointer, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance))
            return;
        _appearance.SetData(uid, PinpointerVisuals.IsActive, pinpointer.IsActive, appearance);
        _appearance.SetData(uid, PinpointerVisuals.TargetDistance, pinpointer.DistanceToTarget, appearance);
    }

    private void OnActivate(EntityUid uid, PinpointerComponent component, ActivateInWorldEvent args)
    {
        TogglePinpointer(uid, component);
    }

    /// <summary>
    ///     Searches the closest object that has a specific component, this entity is then added to the stored targets.
    /// </summary>
    private void LocateTarget(EntityUid uid, PinpointerComponent component,  IComponent selectedComponent,EntityUid? user = null)
    {
        var target = FindTargetFromComponent(uid, selectedComponent.GetType());

        //Don't track or store the target if a fake variant is in the list of tracked targets.
        if (target != null)
        {
            foreach (var storedTarget in component.StoredTargets)
            {
                if (_tagSystem.HasTag(storedTarget, "FakeDisk") && _tagSystem.HasTag(target.Value, "RealDisk"))
                    target = storedTarget;
            }
        }

        StoreTarget(target, uid, component);
        SetTarget(uid, target, component, user);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // because target or pinpointer can move
        // we need to update pinpointers arrow each frame
        var query = EntityQueryEnumerator<PinpointerComponent>();
        while (query.MoveNext(out var uid, out var pinpointer))
        {
            UpdateDirectionToTarget(uid, pinpointer);
        }
    }

    /// <summary>
    ///     Try to find the closest entity from whitelist on a current map
    ///     Will return null if can't find anything
    /// </summary>
    private EntityUid? FindTargetFromComponent(EntityUid uid, Type whitelist, TransformComponent? transform = null)
    {
        _xformQuery.Resolve(uid, ref transform, false);

        if (transform == null)
            return null;

        // sort all entities in distance increasing order
        var mapId = transform.MapID;
        var l = new SortedList<float, EntityUid>();
        var worldPos = _transform.GetWorldPosition(transform);

        foreach (var (otherUid, _) in EntityManager.GetAllComponents(whitelist))
        {
            if (!_xformQuery.TryGetComponent(otherUid, out var compXform) || compXform.MapID != mapId)
                continue;

            var dist = (_transform.GetWorldPosition(compXform) - worldPos).LengthSquared();
            l.TryAdd(dist, otherUid);
        }

        // return uid with a smallest distance
        return l.Count > 0 ? l.First().Value : null;
    }

    /// <summary>
    ///     Set pinpointers target to track
    /// </summary>
    public void SetTarget(EntityUid uid, EntityUid? target, PinpointerComponent? pinpointer = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;

        if (pinpointer.Target == target)
        {
            UpdateDirectionToTarget(uid, pinpointer);
            return;
        }

        pinpointer.Target = target;

        //Searches for the name of the tracked entity
        if (pinpointer.Target != null)
        {
            pinpointer.TargetName = Identity.Name(pinpointer.Target.Value, EntityManager);
        }

        if (user != null && pinpointer.TargetName != null)
        {
            _popup.PopupEntity(
                target == null
                    ? Loc.GetString("targeting-pinpointer-failed")
                    : Loc.GetString("targeting-pinpointer-succeeded", ("target", pinpointer.TargetName)), user.Value, user.Value);
        }

        //Turns on the pinpointer if the target is changed through the verb menu.
        if (!pinpointer.IsActive && user != null)
        {
            TogglePinpointer(uid, pinpointer);
        }

        UpdateDirectionToTarget(uid, pinpointer);
    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    private void UpdateDirectionToTarget(EntityUid uid, PinpointerComponent pinpointer)
    {
        if (!pinpointer.IsActive)
            return;

        var target = pinpointer.Target;
        if (target == null || !EntityManager.EntityExists(target.Value))
        {
            //Updates appearance when currently tracking entity gets deleted or moves off-station
            if (pinpointer.DistanceToTarget != Distance.Unknown)
            {
                SetDistance(uid, Distance.Unknown, pinpointer);
                UpdateAppearance(uid,pinpointer);
            }
            return;
        }

        var dirVec = CalculateDirection(uid, target.Value);
        var oldDist = pinpointer.DistanceToTarget;
        if (dirVec != null)
        {
            var angle = dirVec.Value.ToWorldAngle();
            TrySetArrowAngle(uid, angle, pinpointer);
            var dist = CalculateDistance(dirVec.Value, pinpointer);
            SetDistance(uid, dist, pinpointer);
        }
        else
        {
            SetDistance(uid, Distance.Unknown, pinpointer);
        }
        if (oldDist != pinpointer.DistanceToTarget)
            UpdateAppearance(uid, pinpointer);
    }

    /// <summary>
    ///     Calculate direction from pinUid to trgUid
    /// </summary>
    /// <returns>Null if failed to calculate distance between two entities</returns>
    private Vector2? CalculateDirection(EntityUid pinUid, EntityUid trgUid)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        // check if entities have transform component
        if (!xformQuery.TryGetComponent(pinUid, out var pin))
            return null;
        if (!xformQuery.TryGetComponent(trgUid, out var trg))
            return null;

        // check if they are on same map
        if (pin.MapID != trg.MapID)
            return null;

        // get world direction vector
        var dir = _transform.GetWorldPosition(trg, xformQuery) - _transform.GetWorldPosition(pin, xformQuery);
        return dir;
    }

    private Distance CalculateDistance(Vector2 vec, PinpointerComponent pinpointer)
    {
        var dist = vec.Length();
        if (dist <= pinpointer.ReachedDistance)
            return Distance.Reached;
        else if (dist <= pinpointer.CloseDistance)
            return Distance.Close;
        else if (dist <= pinpointer.MediumDistance)
            return Distance.Medium;
        else
            return Distance.Far;
    }

    /// <summary>
    ///     Clears the list with stored targets and turns off the pinpointer.
    /// </summary>
    private void RemoveAllStoredTargets(EntityUid uid, PinpointerComponent component, EntityUid? user)
    {
        component.StoredTargets.Clear();
        if (component.IsActive)
        {
            TogglePinpointer(uid, component);
        }
    }

    /// <summary>
    /// Removes a target from the target list if the Trackable component is removed.
    /// </summary>
    public void RemoveDeletedTargets(EntityUid uid, PinpointerComponent component)
    {
        component.StoredTargets.Remove(uid);
    }

    /// <summary>
    ///     Adds a verb that allows the user to search for the closest target containing a certain component, a verb that
    ///     allows the user to select any of the stored targets and a verb that allows the user to clear the stored targets.
    /// </summary>
    private void OnPinpointerVerb(EntityUid uid, PinpointerComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || args.Hands == null)
            return;

        var storedOrder = 0;

        //Adds the closest target verb if there is at least 1 stored component, there is no need to show an empty list.
        if (component.Components.Count > 0)
        {
            foreach (var targetComponent in component.Components)
            {
                args.Verbs.Add(new Verb()
                {
                    Text = Loc.GetString(component.ComponentNames[storedOrder]),
                    Act = () => LocateTarget(uid, component, targetComponent.Value.Component,args.User),
                    Priority = 100,
                    Category = VerbCategory.SearchClosest,
                });
                storedOrder++;
            }
        }

        storedOrder = 0;

        //Adds the target selection verb if there is more than 1 stored target, no need to show a list with only 1 target
        //because it will be targeted by default
        if (component.StoredTargets.Count > 1)
        {
            foreach (var target in component.StoredTargets)
            {
                // Adds a number in front of a name to order the list based on order added
                var storedPrefix = Loc.GetString("prefix-pinpointer-targets", ("storedOrder", storedOrder));
                storedOrder++;

                args.Verbs.Add(new Verb()
                {
                    Text = storedPrefix + Identity.Name(target, EntityManager),
                    Act = () => SetTarget(uid, target, component, args.User),
                    Priority = 50,
                    Category = VerbCategory.SelectTarget
                });
            }
        }

        //Adds the stored target reset verb if there is at least 1 stored target,
        //no need to reset if there is no stored targets.
        if (component.StoredTargets.Count > 0)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("reset-pinpointer-targets"),
                Act = () => RemoveAllStoredTargets(uid, component, args.User),
                Category = null,
                Priority = 25
            });
        }

    }
}
