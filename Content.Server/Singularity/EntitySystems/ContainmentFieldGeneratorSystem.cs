using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Singularity.Events;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Singularity.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentFieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AppearanceSystem _visualizer = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, StartCollideEvent>(HandleGeneratorCollide);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ReAnchorEvent>(OnReanchorEvent);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, EventHorizonAttemptConsumeEntityEvent>(PreventBreach);
        SubscribeLocalEvent<ContainmentFieldGeneratorComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ContainmentFieldGeneratorComponent>();
        while (query.MoveNext(out var uid, out var generator))
        {
            if (generator.PowerBuffer <= 0) //don't drain power if there's no power, or if it's somehow less than 0.
                continue;

            generator.Accumulator += frameTime;

            if (generator.Accumulator >= generator.Threshold)
            {
                LosePower((uid, generator), generator.PowerLoss);
                generator.Accumulator -= generator.Threshold;
            }
        }
    }

    #region Events

    private void OnMapInit(Entity<ContainmentFieldGeneratorComponent> generator, ref MapInitEvent args)
    {
        if (generator.Comp.Enabled)
            ChangeFieldVisualizer(generator);
    }

    /// <summary>
    /// A generator receives power from a source colliding with it.
    /// </summary>
    private void HandleGeneratorCollide(Entity<ContainmentFieldGeneratorComponent> generator, ref StartCollideEvent args)
    {
        if (args.OtherFixtureId == generator.Comp.SourceFixtureId &&
            _tags.HasTag(args.OtherEntity, generator.Comp.IDTag))
        {
            ReceivePower(generator.Comp.PowerReceived, generator);
            generator.Comp.Accumulator = 0f;
        }
    }

    private void OnExamine(EntityUid uid, ContainmentFieldGeneratorComponent component, ExaminedEvent args)
    {
        if (component.Enabled)
            args.PushMarkup(Loc.GetString("comp-containment-on"));

        else
            args.PushMarkup(Loc.GetString("comp-containment-off"));
    }

    private void OnActivate(Entity<ContainmentFieldGeneratorComponent> generator, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(generator, out TransformComponent? transformComp) && transformComp.Anchored)
        {
            if (!generator.Comp.Enabled)
                TurnOn(generator);
            else if (generator.Comp.Enabled && generator.Comp.IsConnected)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-containment-toggle-warning"), args.User, args.User, PopupType.LargeCaution);
                return;
            }
            else
                TurnOff(generator);
        }
        args.Handled = true;
    }

    private void OnAnchorChanged(Entity<ContainmentFieldGeneratorComponent> generator, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            RemoveConnections(generator);
    }

    private void OnReanchorEvent(Entity<ContainmentFieldGeneratorComponent> generator, ref ReAnchorEvent args)
    {
        GridCheck(generator);
    }

    private void OnUnanchorAttempt(EntityUid uid, ContainmentFieldGeneratorComponent component,
        UnanchorAttemptEvent args)
    {
        if (component.Enabled || component.IsConnected)
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-containment-anchor-warning"), args.User, args.User, PopupType.LargeCaution);
            args.Cancel();
        }
    }

    private void TurnOn(Entity<ContainmentFieldGeneratorComponent> generator)
    {
        generator.Comp.Enabled = true;
        ChangeFieldVisualizer(generator);
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-turned-on"), generator);
    }

    private void TurnOff(Entity<ContainmentFieldGeneratorComponent> generator)
    {
        generator.Comp.Enabled = false;
        ChangeFieldVisualizer(generator);
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-turned-off"), generator);
    }

    private void OnComponentRemoved(Entity<ContainmentFieldGeneratorComponent> generator, ref ComponentRemove args)
    {
        RemoveConnections(generator);
    }

    /// <summary>
    /// Deletes the fields and removes the respective connections for the generators.
    /// </summary>
    private void RemoveConnections(Entity<ContainmentFieldGeneratorComponent> generator)
    {
        var (uid, component) = generator;
        foreach (var (direction, value) in component.Connections)
        {
            foreach (var field in value.Item2)
            {
                QueueDel(field);
            }
            value.Item1.Comp.Connections.Remove(direction.GetOpposite());

            if (value.Item1.Comp.Connections.Count == 0) //Change isconnected only if there's no more connections
            {
                value.Item1.Comp.IsConnected = false;
                ChangeOnLightVisualizer(value.Item1);
            }

            ChangeFieldVisualizer(value.Item1);
        }
        component.Connections.Clear();
        if (component.IsConnected)
            _popupSystem.PopupEntity(Loc.GetString("comp-containment-disconnected"), uid, PopupType.LargeCaution);
        component.IsConnected = false;
        ChangeOnLightVisualizer(generator);
        ChangeFieldVisualizer(generator);
        _adminLogger.Add(LogType.FieldGeneration, LogImpact.Medium, $"{ToPrettyString(uid)} lost field connections"); // Ideally LogImpact would depend on if there is a singulo nearby
    }

    #endregion

    #region Connections

    /// <summary>
    /// Stores power in the generator. If it hits the threshold, it tries to establish a connection.
    /// </summary>
    /// <param name="power">The power that this generator received from the collision in <see cref="HandleGeneratorCollide"/></param>
    public void ReceivePower(int power, Entity<ContainmentFieldGeneratorComponent> generator)
    {
        var component = generator.Comp;
        component.PowerBuffer += power;

        var genXForm = Transform(generator);

        if (component.PowerBuffer >= component.PowerMinimum)
        {
            var directions = Enum.GetValues<Direction>().Length;
            for (int i = 0; i < directions-1; i+=2)
            {
                var dir = (Direction)i;

                if (component.Connections.ContainsKey(dir))
                    continue; // This direction already has an active connection

                TryGenerateFieldConnection(dir, generator, genXForm);
            }
        }

        ChangePowerVisualizer(power, generator);
    }

    public void LosePower(Entity<ContainmentFieldGeneratorComponent> generator, int power)
    {
        var component = generator.Comp;
        component.PowerBuffer -= power;

        if (component.PowerBuffer < component.PowerMinimum && component.Connections.Count != 0)
        {
            RemoveConnections(generator);
        }

        ChangePowerVisualizer(power, generator);
    }

    /// <summary>
    /// This will attempt to establish a connection of fields between two generators.
    /// If all the checks pass and fields spawn, it will store this connection on each respective generator.
    /// </summary>
    /// <param name="dir">The field generator establishes a connection in this direction.</param>
    /// <param name="generator">The field generator component</param>
    /// <param name="gen1XForm">The transform component for the first generator</param>
    /// <returns></returns>
    private bool TryGenerateFieldConnection(Direction dir, Entity<ContainmentFieldGeneratorComponent> generator, TransformComponent gen1XForm)
    {
        var component = generator.Comp;
        if (!component.Enabled)
            return false;

        if (!gen1XForm.Anchored)
            return false;

        var genWorldPosRot = _transformSystem.GetWorldPositionRotation(gen1XForm);
        var dirRad = dir.ToAngle() + genWorldPosRot.WorldRotation; //needs to be like this for the raycast to work properly

        var ray = new CollisionRay(genWorldPosRot.WorldPosition, dirRad.ToVec(), component.CollisionMask);
        var rayCastResults = _physics.IntersectRay(gen1XForm.MapID, ray, component.MaxLength, generator, false);
        var genQuery = GetEntityQuery<ContainmentFieldGeneratorComponent>();

        RayCastResults? closestResult = null;

        foreach (var result in rayCastResults)
        {
            if (genQuery.HasComponent(result.HitEntity))
                closestResult = result;

            break;
        }
        if (closestResult == null)
            return false;

        var ent = closestResult.Value.HitEntity;

        if (!TryComp<ContainmentFieldGeneratorComponent>(ent, out var otherFieldGeneratorComponent) ||
            otherFieldGeneratorComponent == component ||
            !TryComp<PhysicsComponent>(ent, out var collidableComponent) ||
            collidableComponent.BodyType != BodyType.Static ||
            gen1XForm.ParentUid != Transform(ent).ParentUid)
        {
            return false;
        }

        var otherFieldGenerator = (ent, otherFieldGeneratorComponent);
        var fields = GenerateFieldConnection(generator, otherFieldGenerator);

        component.Connections[dir] = (otherFieldGenerator, fields);
        otherFieldGeneratorComponent.Connections[dir.GetOpposite()] = (generator, fields);
        ChangeFieldVisualizer(otherFieldGenerator);

        if (!component.IsConnected)
        {
            component.IsConnected = true;
            ChangeOnLightVisualizer(generator);
        }

        if (!otherFieldGeneratorComponent.IsConnected)
        {
            otherFieldGeneratorComponent.IsConnected = true;
            ChangeOnLightVisualizer(otherFieldGenerator);
        }

        ChangeFieldVisualizer(generator);
        UpdateConnectionLights(generator);
        _popupSystem.PopupEntity(Loc.GetString("comp-containment-connected"), generator);
        return true;
    }

    /// <summary>
    /// Spawns fields between two generators if the <see cref="TryGenerateFieldConnection"/> finds two generators to connect.
    /// </summary>
    /// <param name="firstGen">The source field generator</param>
    /// <param name="secondGen">The second generator that the source is connected to</param>
    /// <returns></returns>
    private List<EntityUid> GenerateFieldConnection(Entity<ContainmentFieldGeneratorComponent> firstGen, Entity<ContainmentFieldGeneratorComponent> secondGen)
    {
        var fieldList = new List<EntityUid>();
        var gen1Coords = Transform(firstGen).Coordinates;
        var gen2Coords = Transform(secondGen).Coordinates;

        var delta = (gen2Coords - gen1Coords).Position;
        var dirVec = delta.Normalized();
        var stopDist = delta.Length();
        var currentOffset = dirVec;
        while (currentOffset.Length() < stopDist)
        {
            var currentCoords = gen1Coords.Offset(currentOffset);
            var newField = Spawn(firstGen.Comp.CreatedField, currentCoords);

            var fieldXForm = Transform(newField);
            _transformSystem.SetParent(newField, fieldXForm, firstGen);
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
    public void UpdateConnectionLights(Entity<ContainmentFieldGeneratorComponent> generator)
    {
        if (_light.TryGetLight(generator, out var pointLightComponent))
        {
            _light.SetEnabled(generator, generator.Comp.Connections.Count > 0, pointLightComponent);
        }
    }

    /// <summary>
    /// Checks to see if this or the other gens connected to a new grid. If they did, remove connection.
    /// </summary>
    public void GridCheck(Entity<ContainmentFieldGeneratorComponent> generator)
    {
        var xFormQuery = GetEntityQuery<TransformComponent>();

        foreach (var (_, generators) in generator.Comp.Connections)
        {
            var gen1ParentGrid = xFormQuery.GetComponent(generator).ParentUid;
            var gent2ParentGrid = xFormQuery.GetComponent(generators.Item1).ParentUid;

            if (gen1ParentGrid != gent2ParentGrid)
                RemoveConnections(generator);
        }
    }

    #endregion

    #region VisualizerHelpers
    /// <summary>
    /// Check if a fields power falls between certain ranges to update the field gen visual for power.
    /// </summary>
    /// <param name="power"></param>
    /// <param name="generator"></param>
    private void ChangePowerVisualizer(int power, Entity<ContainmentFieldGeneratorComponent> generator)
    {
        var component = generator.Comp;
        _visualizer.SetData(generator, ContainmentFieldGeneratorVisuals.PowerLight, component.PowerBuffer switch
        {
            <= 0 => PowerLevelVisuals.NoPower,
            >= 25 => PowerLevelVisuals.HighPower,
            _ => (component.PowerBuffer < component.PowerMinimum)
                ? PowerLevelVisuals.LowPower
                : PowerLevelVisuals.MediumPower
        });
    }

    /// <summary>
    /// Check if a field has any or no connections and if it's enabled to toggle the field level light
    /// </summary>
    /// <param name="generator"></param>
    private void ChangeFieldVisualizer(Entity<ContainmentFieldGeneratorComponent> generator)
    {
        _visualizer.SetData(generator, ContainmentFieldGeneratorVisuals.FieldLight, generator.Comp.Connections.Count switch
        {
            >1 => FieldLevelVisuals.MultipleFields,
            1 => FieldLevelVisuals.OneField,
            _ => generator.Comp.Enabled ? FieldLevelVisuals.On : FieldLevelVisuals.NoLevel
        });
    }

    private void ChangeOnLightVisualizer(Entity<ContainmentFieldGeneratorComponent> generator)
    {
        _visualizer.SetData(generator, ContainmentFieldGeneratorVisuals.OnLight, generator.Comp.IsConnected);
    }
    #endregion

    /// <summary>
    /// Prevents singularities from breaching containment if the containment field generator is connected.
    /// </summary>
    /// <param name="uid">The entity the singularity is trying to eat.</param>
    /// <param name="comp">The containment field generator the singularity is trying to eat.</param>
    /// <param name="args">The event arguments.</param>
    private void PreventBreach(EntityUid uid, ContainmentFieldGeneratorComponent comp, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        if (args.Cancelled)
            return;
        if (comp.IsConnected && !args.EventHorizon.CanBreachContainment)
            args.Cancelled = true;
    }
}
