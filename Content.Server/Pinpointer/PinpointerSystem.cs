using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using System.Linq;
using System.Numerics;
using Content.Server.Popups;
using Robust.Shared.Utility;
using Content.Server.Shuttles.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;

namespace Content.Server.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<PinpointerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<FTLCompletedEvent>(OnLocateTarget);
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

        if (component.StoredTargets.Count == 0)
            LocateTarget(uid, component);
    }

    private void OnLocateTarget(ref FTLCompletedEvent ev)
    {
        // This feels kind of expensive, but it only happens once per hyperspace jump

        // todo: ideally, you would need to raise this event only on jumped entities
        // this code update ALL pinpointers in game
        var query = EntityQueryEnumerator<PinpointerComponent>();

        while (query.MoveNext(out var uid, out var pinpointer))
        {
            if (pinpointer.CanRetarget)
                continue;

            LocateTarget(uid, pinpointer);
        }
    }

    /// <summary>
    /// Searches the closest object that has a specific component for every component the pinpointer has stored.
    /// This entity is then added to the stored targets.
    /// </summary>
    private void LocateTarget(EntityUid uid, PinpointerComponent component, EntityUid? user = null, string? selectedComponent = null)
    {
        // try to find target from whitelist
        var targetedComponent = "";
        if (selectedComponent == null)
        {
            targetedComponent = component.Components[0];
        }
        else
        {
            targetedComponent = selectedComponent;
        }

        if (!EntityManager.ComponentFactory.TryGetRegistration(targetedComponent, out var reg))
        {
            Log.Error($"Unable to find component registration for {targetedComponent} for pinpointer!");
            DebugTools.Assert(false);
            return;
        }

        var target = FindTargetFromComponent(uid, reg.Type);

        //Adds the target to the stored targets if it's not already in there.
        if(target != null && !component.StoredTargets.Contains(target.Value) && user != null)
        {
            component.StoredTargets.Add(target.Value);
        }

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
            return;

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
    /// Clears the list with stored targets and turns off the pinpointer.
    /// </summary>
    private void DeleteStoredTargets(EntityUid uid, PinpointerComponent component, EntityUid? user)
    {
        component.StoredTargets.Clear();
        if (component.IsActive)
        {
            TogglePinpointer(uid, component);
        }

    }

    /// <summary>
    /// Adds the verb that allows the user to select any of the stored targets.
    /// Additionally adds a verb that allows the user to clear the stored targets.
    /// </summary>
    private void OnPinpointerVerb(EntityUid uid, PinpointerComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || args.Hands == null)
            return;

        //Adds the verb if there is at least 1 stored component
        if (component.Components.Count > 0)
        {
            foreach (var targetComponent in component.Components)
            {
                args.Verbs.Add(new Verb()
                {
                    Text = Loc.GetString(component.ComponentNames[component.Components.IndexOf(targetComponent)]),
                    Act = () => LocateTarget(uid, component, args.User, targetComponent),
                    Priority = 100,
                    Category = VerbCategory.SearchClosest,
                });
            }
        }

        var storedOrder = 0;

        //Adds the verb if there is more than 1 stored target.
        if (component.StoredTargets.Count > 1)
        {
            foreach (var target in component.StoredTargets)
            {
                if (Deleted(target))
                {
                    continue;
                }
                // Adds a number in front of a name to order the list based on order added
                var storedPrefix = Loc.GetString("prefix-pinpointer-targets", ("storedOrder", storedOrder));
                storedOrder++;

                args.Verbs.Add(new Verb()
                {
                    Text = Loc.GetString(storedPrefix + Identity.Name(target, EntityManager) ),
                    Act = () => SetTarget(uid, target, component, args.User),
                    Priority = 50,
                    Category = VerbCategory.SelectTarget
                });
            }
        }

        //Adds the ver if there is at least 1 stored target
        if (component.StoredTargets.Count > 0)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Reset targets"),
                Act = () => DeleteStoredTargets(uid, component, args.User),
                Category = null,
                Priority = 25
            });
        }

    }
}
