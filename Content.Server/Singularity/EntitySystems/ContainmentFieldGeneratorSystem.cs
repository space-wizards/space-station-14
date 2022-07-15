using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using System.Diagnostics.CodeAnalysis;
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

    private void OnUnanchorAttempt(EntityUid uid, ContainmentFieldGeneratorComponent component, UnanchorAttemptEvent args)
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
        DeleteFields(component);
    }

    private void OnAnchorChanged(EntityUid uid, ContainmentFieldGeneratorComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            DeleteFields(component);
        }
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

    //TODO: Get rid of this
    private void HandleFieldCollide(EntityUid uid, ContainmentFieldComponent component, StartCollideEvent args)
    {

    }

    public void ReceivePower(int power, ContainmentFieldGeneratorComponent component)
    {
        component.PowerBuffer += power;

        if (component.PowerBuffer >= component.Power)
        {
            TryGenerateFieldConnection(ref component.Connection1, component);
            TryGenerateFieldConnection(ref component.Connection2, component);
        }
    }

    public void UpdateConnectionLights(ContainmentFieldGeneratorComponent component)
    {
        if (EntityManager.TryGetComponent<PointLightComponent>(component.Owner, out var pointLightComponent))
        {
            bool hasAnyConnection = component.Connection1 != null || component.Connection2 != null;
            pointLightComponent.Enabled = hasAnyConnection;
        }
    }

    public void RemoveConnection(ContainmentFieldGeneratorComponent component)
    {
        if (component.Connection1?.Item2 == component.Fields)
        {
            component.Connection1 = null;
            component.IsConnected = false;
            UpdateConnectionLights(component);
        }
        else if (component.Connection2?.Item2 == component.Fields)
        {
            component.Connection2 = null;
            component.IsConnected = false;
            UpdateConnectionLights(component);
        }
        else if (component.Fields != null)
        {
            Logger.Error("RemoveConnection called on Containment Field Generator with a connection that can't be found in its connections.");
        }
    }

    private bool TryGenerateFieldConnection([NotNullWhen(true)] ref Tuple<Angle, List<EntityUid>>? propertyFieldTuple,ContainmentFieldGeneratorComponent component)
    {
        if (propertyFieldTuple != null) return false;
        if (!component.Enabled) return false; //don't gen a field unless it's on

        var gen1XForm = Transform(component.Owner);
        if (!gen1XForm.Anchored) return false;
        var gen1CardinalDirAngle = gen1XForm.WorldRotation;
        var rad1 = Direction.South.ToAngle() + gen1CardinalDirAngle; //This fires east
        var rad2 = Direction.West.ToAngle() + gen1CardinalDirAngle; //This fires south
        var rad3 = Direction.North.ToAngle() + gen1CardinalDirAngle; //This fires west
        var rad4 = Direction.East.ToAngle() + gen1CardinalDirAngle; //This fires north

        //TODO: Finding the generators seems fine, need to prevent it from going through a wall, however.
        //TODO: There is still an issue with x2 fields spawning on the generator that has null generators, but has connections
        //Though I just double checked with a 1x1 and there were no field gens
        //TODO: Also extra issue with the last node (D) despawning the connection between A-B.
        //TODO: Maybe change from the spawn each to the singular spawn with texture wrapper.
        foreach (var direction in new[] { rad1, rad2, rad3, rad4 })
        {
            if (component.Connection1?.Item1 == direction || component.Connection2?.Item1 == direction) continue;

            var ray = new CollisionRay(gen1XForm.MapPosition.Position, direction.ToVec(), component.CollisionMask);
            var rayCastResults = _physics.IntersectRay(gen1XForm.MapID, ray, component.MaxLength, component.Owner, false).ToList();

            if (!rayCastResults.Any()) continue;

            RayCastResults? closestResult = null;
            foreach (var res in rayCastResults)
            {
                if (HasComp<ContainmentFieldGeneratorComponent>(res.HitEntity))
                {
                    closestResult = res;
                }
                break;
            }
            if (closestResult == null) continue;

            var ent = closestResult.Value.HitEntity;

            //TODO: Something could be wrong with this
            if (!TryComp<ContainmentFieldGeneratorComponent?>(ent, out var fieldGeneratorComponent) ||
                fieldGeneratorComponent.Owner == component.Owner ||
                !HasFreeConnections(fieldGeneratorComponent) ||
                IsConnectedWith(component, fieldGeneratorComponent) ||
                !TryComp<PhysicsComponent?>(ent, out var collidableComponent) ||
                collidableComponent.BodyType != BodyType.Static)
            {
                continue;
            }

            //rework this I don't think it will work out
            //TODO: One gen always appears with no connections
            component.Generator1 = component.Owner;
            if (component.Generator2 == fieldGeneratorComponent.Owner) //new addition, come back to this
                continue;

            component.Generator2 = fieldGeneratorComponent.Owner;

            //The spawning works fine
            GenerateConnection(component);
            //TODO: Really need to reapproach the tuple approach
            propertyFieldTuple = new Tuple<Angle, List<EntityUid>>(direction, component.Fields);
            if (fieldGeneratorComponent.Connection1 == null)
            {
                fieldGeneratorComponent.Connection1 = new Tuple<Angle, List<EntityUid>>(direction.Opposite(), component.Fields);
                if (!fieldGeneratorComponent.IsConnected)
                    fieldGeneratorComponent.IsConnected = true;
            }
            else if (fieldGeneratorComponent.Connection2 == null)
            {
                fieldGeneratorComponent.Connection2 = new Tuple<Angle, List<EntityUid>>(direction.Opposite(), component.Fields);
                if (!fieldGeneratorComponent.IsConnected)
                    fieldGeneratorComponent.IsConnected = true;
            }
            else
            {
                Logger.Error("When trying to connect two Containment Field Generators, the second one already had two connection but the check didn't catch it");
            }

            component.IsConnected = true;
            UpdateConnectionLights(component);
            return true;
        }

        return false;
    }

    private void GenerateConnection(ContainmentFieldGeneratorComponent component)
    {
        if (component.Generator1 == null || component.Generator2 == null)
            return;

        var gen1Coords = Transform(component.Generator1.Value).Coordinates;
        var gen2Coords = Transform(component.Generator2.Value).Coordinates;

        if (gen1Coords == gen2Coords)
            return;

        var delta = (gen2Coords - gen1Coords).Position;
        var dirVec = delta.Normalized;
        var stopDist = delta.Length;
        var currentOffset = dirVec;
        while (currentOffset.Length < stopDist)
        {
            var currentCoords = gen1Coords.Offset(currentOffset);
            var newField = Spawn(component.CreatedField, currentCoords);

            var fieldXForm = Transform(newField);
            fieldXForm.AttachParent(component.Generator1.Value);
            if (dirVec.GetDir() == Direction.East || dirVec.GetDir() == Direction.West)
            {
                var angle = fieldXForm.LocalPosition.ToAngle();
                var rotateBy90 = angle.Degrees + 90;
                var rotatedAngle = Angle.FromDegrees(rotateBy90);

                fieldXForm.LocalRotation = rotatedAngle;
            }

            component.Fields.Add(newField);
            currentOffset += dirVec;
        }
    }

    private void DeleteFields(ContainmentFieldGeneratorComponent component)
    {

        if (component.Connection1?.Item2 != null)
        {
            foreach (var field in component.Connection1.Item2)
            {
                QueueDel(field);
            }

            component.Connection1 = null;
        }

        if (component.Connection2?.Item2 != null)
        {
            foreach (var field in component.Connection2.Item2)
            {
                QueueDel(field);
            }

            component.Connection2 = null;
        }

        foreach (var field in component.Fields)
        {
            QueueDel(field);
        }
        component.Fields.Clear();

        component.Generator1 = null;
        component.Generator2 = null;
    }

    public bool CanRepel(SharedSingularityComponent toRepel, ContainmentFieldGeneratorComponent component)
    {
        //component.Connection1?.Item2?.CanRepel(toRepel) == true || component.Connection2?.Item2?.CanRepel(toRepel) == true;
        return false;
    }

    public bool IsConnectedWith(ContainmentFieldGeneratorComponent mainGen, ContainmentFieldGeneratorComponent connectedGen)
    {
        return connectedGen == mainGen || mainGen.Generator1 == connectedGen.Owner ||
               mainGen.Generator2 == connectedGen.Owner;
    }

    public bool HasFreeConnections(ContainmentFieldGeneratorComponent component)
    {
        return component.Connection1 == null || component.Connection2 == null;
    }
}
