using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using System.Linq;
using Content.Server.Popups;
using Content.Shared.Construction.Components;
using Content.Shared.Interaction;
using Robust.Shared.Player;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentFieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, StartCollideEvent>(HandleGeneratorCollide);

        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, InteractHandEvent>(OnInteract);

        SubscribeLocalEvent<ParticleProjectileComponent, StartCollideEvent>(HandleParticleCollide);

        SubscribeLocalEvent<ContainmentFieldComponent, StartCollideEvent>(HandleFieldCollide);
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
                _popupSystem.PopupEntity(Loc.GetString("comp-containment-anchor-warning"), args.User, Filter.Entities(args.User));
                return;
            }
            else
                TurnOff(component);
        }
        args.Handled = true;
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
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-turned-on"), component.Owner,
            Filter.Pvs(component.Owner));
    }

    private void TurnOff(ContainmentFieldGeneratorComponent component)
    {
        component.Enabled = false;
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-turned-off"), component.Owner,
            Filter.Pvs(component.Owner));
    }

    private void OnComponentRemoved(EntityUid uid, ContainmentFieldGeneratorComponent component, ComponentRemove args)
    {
        DeleteFields(component);
    }

    private void OnAnchorChanged(EntityUid uid, ContainmentFieldGeneratorComponent component,
        ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            DeleteFields(component);
    }

    //TODO: put into its own PA system?
    private void HandleParticleCollide(EntityUid uid, ParticleProjectileComponent component, StartCollideEvent args)
    {
        if (EntityManager.TryGetComponent<SingularityGeneratorComponent?>(args.OtherFixture.Body.Owner, out var singularityGeneratorComponent))
        {
            singularityGeneratorComponent.Power += component.State switch
            {
                ParticleAcceleratorPowerState.Standby => 0,
                ParticleAcceleratorPowerState.Level0 => 1,
                ParticleAcceleratorPowerState.Level1 => 2,
                ParticleAcceleratorPowerState.Level2 => 4,
                ParticleAcceleratorPowerState.Level3 => 8,
                _ => 0
            };
            EntityManager.QueueDeleteEntity(uid);
        }
    }

    private void HandleGeneratorCollide(EntityUid uid, ContainmentFieldGeneratorComponent component, StartCollideEvent args)
    {
        if (_tags.HasTag(args.OtherFixture.Body.Owner, component.IDTag))
        {
            ReceivePower(component.Power, component);
        }
    }

    //TODO: Rework this to player bouncing
    private void HandleFieldCollide(EntityUid uid, ContainmentFieldComponent component, StartCollideEvent args)
    {

    }

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

    public void UpdateConnectionLights(ContainmentFieldGeneratorComponent component)
    {
        if (EntityManager.TryGetComponent<PointLightComponent>(component.Owner, out var pointLightComponent))
        {
            bool hasAnyConnection = component.Connections != null;
            pointLightComponent.Enabled = hasAnyConnection;
        }
    }

    private bool TryGenerateFieldConnection(Direction dir, ContainmentFieldGeneratorComponent component)
    {
        if (!component.Enabled) return false;

        var genXForm = Transform(component.Owner);
        if (!genXForm.Anchored) return false;

        var genCardinalDirAngle = genXForm.WorldRotation;
        var dirRad = dir.ToAngle() + genCardinalDirAngle;

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
        return true;
    }

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

    private void DeleteFields(ContainmentFieldGeneratorComponent component)
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
    }

    public bool CanRepel(SharedSingularityComponent toRepel, ContainmentFieldGeneratorComponent component)
    {
        //component.Connection1?.Item2?.CanRepel(toRepel) == true || component.Connection2?.Item2?.CanRepel(toRepel) == true;
        return false;
    }
}
