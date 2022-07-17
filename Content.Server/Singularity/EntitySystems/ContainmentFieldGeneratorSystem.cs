using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using System.Linq;
using Content.Server.Popups;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Robust.Shared.Player;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentFieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, StartCollideEvent>(HandleGeneratorCollide);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
    }

    #region Events

    /// <summary>
    /// A generator receives power from a source colliding with it.
    /// </summary>
    private void HandleGeneratorCollide(EntityUid uid, ContainmentFieldGeneratorComponent component, StartCollideEvent args)
    {
        if (_tags.HasTag(args.OtherFixture.Body.Owner, component.IDTag))
        {
            ReceivePower(component.Power, component);
        }
    }

    private void OnExamine(EntityUid uid, ContainmentFieldGeneratorComponent component, ExaminedEvent args)
    {
        if (component.Enabled)
            args.PushMarkup(Loc.GetString("comp-containment-on"));

        else
            args.PushMarkup(Loc.GetString("comp-containment-off"));
    }

    private void OnInteract(EntityUid uid, ContainmentFieldGeneratorComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(component.Owner, out TransformComponent? transformComp) && transformComp.Anchored)
        {
            if (!component.Enabled)
                TurnOn(component);
            else if (component.Enabled && component.IsConnected)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-containment-toggle-warning"), args.User, Filter.Entities(args.User));
                return;
            }
            else
                TurnOff(component);
        }
        args.Handled = true;
    }

    private void OnAnchorChanged(EntityUid uid, ContainmentFieldGeneratorComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            RemoveConnections(component);
    }

    private void OnUnanchorAttempt(EntityUid uid, ContainmentFieldGeneratorComponent component,
        UnanchorAttemptEvent args)
    {
        if (component.Enabled)
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-containment-anchor-warning"), args.User, Filter.Entities(args.User));
            args.Cancel();
        }
    }

    private void TurnOn(ContainmentFieldGeneratorComponent component)
    {
        component.Enabled = true;
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-turned-on"), component.Owner, Filter.Pvs(component.Owner));
    }

    private void TurnOff(ContainmentFieldGeneratorComponent component)
    {
        component.Enabled = false;
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-turned-off"), component.Owner, Filter.Pvs(component.Owner));
    }

    private void OnComponentRemoved(EntityUid uid, ContainmentFieldGeneratorComponent component, ComponentRemove args)
    {
        RemoveConnections(component);
    }

    /// <summary>
    /// Deletes the fields and removes the respective connections for the generators.
    /// </summary>
    private void RemoveConnections(ContainmentFieldGeneratorComponent component)
    {
        foreach (var (direction, value) in component.Connections)
        {
            foreach (var field in value.Item2)
            {
                QueueDel(field);
            }
            value.Item1.Connections.Remove(direction.GetOpposite());
        }
        component.Connections.Clear();
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-disconnected"), component.Owner, Filter.Pvs(component.Owner));
    }

    #endregion

    #region Connections

        /// <summary>
    /// Stores power in the generator. If it hits the threshold, it tries to establish a connection.
    /// </summary>
    /// <param name="power">The power that this generator received from the collision in <see cref="HandleGeneratorCollide"/></param>
    public void ReceivePower(int power, ContainmentFieldGeneratorComponent component)
    {
        component.PowerBuffer += power;

        if (component.PowerBuffer >= component.Power)
        {
            for (int i = 0; i < 8; i+=2)
            {
                var dir = (Direction)i;

                if (component.Connections.ContainsKey(dir))
                    continue; // This direction already has an active connection

                TryGenerateFieldConnection(dir, component);
            }
        }
    }

    /// <summary>
    /// This will attempt to establish a connection of fields between two generators.
    /// If all the checks pass and fields spawn, it will store this connection on each respective generator.
    /// </summary>
    /// <param name="dir">The field generator establishes a connection in this direction.</param>
    /// <param name="component">The field generator component</param>
    /// <returns></returns>
    private bool TryGenerateFieldConnection(Direction dir, ContainmentFieldGeneratorComponent component)
    {
        if (!component.Enabled) return false;

        var genXForm = Transform(component.Owner);
        if (!genXForm.Anchored) return false;

        var genCardinalDirAngle = genXForm.WorldRotation;
        var dirRad = dir.ToAngle() + genCardinalDirAngle; //needs to be like this for the raycast to work properly

        var ray = new CollisionRay(genXForm.MapPosition.Position, dirRad.ToVec(), component.CollisionMask);
        var rayCastResults = _physics.IntersectRay(genXForm.MapID, ray, component.MaxLength, component.Owner, false).ToList();

        if (!rayCastResults.Any()) return false;

        RayCastResults? closestResult = null;

        foreach (var result in rayCastResults)
        {
            if (HasComp<ContainmentFieldGeneratorComponent>(result.HitEntity))
                closestResult = result;

            break;
        }
        if (closestResult == null) return false;

        var ent = closestResult.Value.HitEntity;

        if (!TryComp<ContainmentFieldGeneratorComponent?>(ent, out var otherFieldGeneratorComponent) ||
            otherFieldGeneratorComponent == component ||
            !TryComp<PhysicsComponent>(ent, out var collidableComponent) ||
            collidableComponent.BodyType != BodyType.Static)
        {
            return false;
        }

        var fields = GenerateFieldConnection(component, otherFieldGeneratorComponent);

        component.Connections[dir] = (otherFieldGeneratorComponent, fields);
        otherFieldGeneratorComponent.Connections[dir.GetOpposite()] = (component, fields);

        if (!component.IsConnected)
            component.IsConnected = true;

        if (!otherFieldGeneratorComponent.IsConnected)
            otherFieldGeneratorComponent.IsConnected = true;

        UpdateConnectionLights(component);
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-connected"), component.Owner, Filter.Pvs(component.Owner));
        return true;
    }

    /// <summary>
    /// Spawns fields between two generators if the <see cref="TryGenerateFieldConnection"/> finds two generators to connect.
    /// </summary>
    /// <param name="firstGenComp">The source field generator</param>
    /// <param name="secondGenComp">The second generator that the source is connected to</param>
    /// <returns></returns>
    private List<EntityUid> GenerateFieldConnection(ContainmentFieldGeneratorComponent firstGenComp, ContainmentFieldGeneratorComponent secondGenComp)
    {
        var fieldList = new List<EntityUid>();
        var gen1Coords = Transform(firstGenComp.Owner).Coordinates;
        var gen2Coords = Transform(secondGenComp.Owner).Coordinates;

        var delta = (gen2Coords - gen1Coords).Position;
        var dirVec = delta.Normalized;
        var stopDist = delta.Length;
        var currentOffset = dirVec;
        while (currentOffset.Length < stopDist)
        {
            var currentCoords = gen1Coords.Offset(currentOffset);
            var newField = Spawn(firstGenComp.CreatedField, currentCoords);

            var fieldXForm = Transform(newField);
            fieldXForm.AttachParent(firstGenComp.Owner);
            if (dirVec.GetDir() == Direction.East || dirVec.GetDir() == Direction.West)
            {
                var angle = fieldXForm.LocalPosition.ToAngle();
                var rotateBy90 = angle.Degrees + 90;
                var rotatedAngle = Angle.FromDegrees(rotateBy90);

                fieldXForm.LocalRotation = rotatedAngle;
            }

            fieldList.Add(newField);
            currentOffset += dirVec;
        }
        return fieldList;
    }

    /// <summary>
    /// Creates a light component for the spawned fields.
    /// </summary>
    public void UpdateConnectionLights(ContainmentFieldGeneratorComponent component)
    {
        if (EntityManager.TryGetComponent<PointLightComponent>(component.Owner, out var pointLightComponent))
        {
            bool hasAnyConnection = component.Connections != null;
            pointLightComponent.Enabled = hasAnyConnection;
        }
    }

    #endregion

    public bool CanRepel(SharedSingularityComponent toRepel, ContainmentFieldGeneratorComponent component)
    {
        return false;
    }
}
