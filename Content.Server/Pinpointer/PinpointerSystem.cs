using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using System.Linq;
using System.Numerics;
using Content.Shared.IdentityManagement;
using Content.Shared.Tag;
using Content.Shared.Verbs;
namespace Content.Server.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<PinpointerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<PinpointerComponent, GetVerbsEvent<Verb>>(OnPinpointerVerb);
    }

    public override bool TogglePinpointer(EntityUid uid, PinpointerComponent? pinpointer = null)
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
    private void LocateTarget(EntityUid uid, PinpointerComponent component,IComponent selectedComponent, EntityUid user)
    {
        var target = FindTargetFromComponent(uid, selectedComponent.GetType());

        //Don't track or store the target if a fake variant is in the list of tracked targets.
        if (target != null)
        {
            foreach (var storedTarget in component.StoredTargets)
            {
                if (_tagSystem.HasTag(storedTarget, "FakeNukeDisk") && _tagSystem.HasTag(target.Value, "RealNukeDisk"))
                    target = storedTarget;
            }
        }

        SetTarget(uid, target, component, user,true);
        StoreTarget(target, uid, component, user);

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
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected override void UpdateDirectionToTarget(EntityUid uid, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;

        if (!pinpointer.IsActive)
            return;

        var target = pinpointer.Target;
        if (target == null || !EntityManager.EntityExists(target.Value))
        {
            //Updates appearance when currently tracking entity gets deleted or moves off-station
            if (pinpointer.DistanceToTarget == Distance.Unknown)
                return;

            SetDistance(uid, Distance.Unknown, pinpointer);
            UpdateAppearance(uid,pinpointer);
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
    private void RemoveAllStoredTargets(EntityUid uid, PinpointerComponent component)
    {
        for (var i = component.StoredTargets.Count - 1; i >= 0; i--)
        {
            var target = component.StoredTargets[i];
            if (!TryComp<TrackableComponent>(target, out var trackable))
                continue;

            //Remove the Trackable component if no other entity is tracking the target.
            if (trackable.TrackedBy.Count == 1)
                RemCompDeferred<TrackableComponent>(target);
            //Remove the pinpointer from the target's TrackedBy list and
            //remove the target from the pinpointer's target list.
            else
            {
                trackable.TrackedBy.Remove(uid);
                RemoveTarget(target, component,uid);
            }
        }

        //Set the current target to null so the arrow doesn't keep pointing towards the last selected target.
        component.Target = null;

        if (component.IsActive)
        {
            TogglePinpointer(uid, component);
        }
    }

    /// <summary>
    ///     Adds a verb that allows the user to search for the closest target containing a certain component, a verb that
    ///     allows the user to select any of the stored targets and a verb that allows the user to clear the stored targets.
    /// </summary>
    private void OnPinpointerVerb(EntityUid uid, PinpointerComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || args.Hands == null)
            return;

        //Adds the closest target verb if there is at least 1 stored component, there is no need to show an empty list.
        if (component.Components.Count > 0)
        {
            foreach (var targetComponent in component.Components)
            {
                args.Verbs.Add(new Verb()
                {
                    Text = Loc.GetString( "name-pinpointer-component-" + targetComponent.Key),
                    Act = () => LocateTarget(uid, component, targetComponent.Value.Component, args.User),
                    Priority = 100,
                    Category = VerbCategory.SearchClosest,
                });
            }
        }

        var storedOrder1 = 0;
        var storedOrder10 = 0;

        //Adds the target selection verb if there is at least 1 stored target, no need to show an empty list.
        if (component.StoredTargets.Count > 0)
        {
            foreach (var target in component.StoredTargets)
            {
                storedOrder1++;
                if (storedOrder1 == 10)
                {
                    storedOrder1 = 0;
                    storedOrder10++;
                }

                // Adds a number in front of a name to order the list based on order added
                var storedPrefix = Loc.GetString("prefix-pinpointer-targets",
                    ("storedOrder10", storedOrder10),("storedOrder1", storedOrder1));

                args.Verbs.Add(new Verb()
                {
                    Text = storedPrefix + " " + Identity.Name(target, EntityManager),
                    Act = () => SetTarget(uid, target, component, args.User, true),
                    Priority = 50,
                    Category = VerbCategory.SelectTarget,
                });
            }
        }

        //Adds the stored target reset verb if there is at least 1 stored target,
        //no need to reset if there are no stored targets.
        if (component.StoredTargets.Count > 0)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("reset-pinpointer-targets"),
                Act = () => RemoveAllStoredTargets(uid, component),
                Category = null,
                Priority = 25
            });
        }
    }
}
