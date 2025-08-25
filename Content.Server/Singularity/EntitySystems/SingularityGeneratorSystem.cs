using Content.Server.ParticleAccelerator.Components;
using Content.Shared.Popups;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server.Singularity.EntitySystems;

public sealed class SingularityGeneratorSystem : SharedSingularityGeneratorSystem
{
    #region Dependencies
    [Dependency] private readonly IViewVariablesManager _vvm = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    #endregion Dependencies

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParticleProjectileComponent, StartCollideEvent>(HandleParticleCollide);

        var vvHandle = _vvm.GetTypeHandler<SingularityGeneratorComponent>();
        vvHandle.AddPath(nameof(SingularityGeneratorComponent.Power), (_, comp) => comp.Power, SetPower);
        vvHandle.AddPath(nameof(SingularityGeneratorComponent.Threshold), (_, comp) => comp.Threshold, SetThreshold);
    }

    public override void Shutdown()
    {
        var vvHandle = _vvm.GetTypeHandler<SingularityGeneratorComponent>();
        vvHandle.RemovePath(nameof(SingularityGeneratorComponent.Power));
        vvHandle.RemovePath(nameof(SingularityGeneratorComponent.Threshold));

        base.Shutdown();
    }


    /// <summary>
    /// Handles what happens when a singularity generator passes its power threshold.
    /// Default behavior is to reset the singularities power level and spawn a singularity.
    /// </summary>
    /// <param name="uid">The uid of the singularity generator.</param>
    /// <param name="comp">The state of the singularity generator.</param>
    private void OnPassThreshold(EntityUid uid, SingularityGeneratorComponent? comp)
    {
        if (!Resolve(uid, ref comp))
            return;

        SetPower(uid, 0, comp);
        Spawn(comp.SpawnPrototype, Transform(uid).Coordinates);
    }

    #region Getters/Setters
    /// <summary>
    /// Setter for <see cref="SingularityGeneratorComponent.Power"/>
    /// If the singularity generator passes its threshold it also spawns a singularity.
    /// </summary>
    /// <param name="comp">The singularity generator component.</param>
    /// <param name="value">The new power level for the generator component to have.</param>
    public void SetPower(EntityUid uid, float value, SingularityGeneratorComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var oldValue = comp.Power;
        if (value == oldValue)
            return;

        comp.Power = value;
        if (comp.Power >= comp.Threshold && oldValue < comp.Threshold)
            OnPassThreshold(uid, comp);
    }

    /// <summary>
    /// Setter for <see cref="SingularityGeneratorComponent.Threshold"/>
    /// If the singularity generator has passed its new threshold it also spawns a singularity.
    /// </summary>
    /// <param name="comp">The singularity generator component.</param>
    /// <param name="value">The new threshold power level for the generator component to have.</param>
    public void SetThreshold(EntityUid uid, float value, SingularityGeneratorComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var oldValue = comp.Threshold;
        if (value == comp.Threshold)
            return;

        comp.Power = value;
        if (comp.Power >= comp.Threshold && comp.Power < oldValue)
            OnPassThreshold(uid, comp);
    }
    #endregion Getters/Setters

    #region Event Handlers
    /// <summary>
    /// Handles PA Particles colliding with a singularity generator.
    /// Adds the power from the particles to the generator.
    /// TODO: Desnowflake this.
    /// </summary>
    /// <param name="uid">The uid of the PA particles have collided with.</param>
    /// <param name="component">The state of the PA particles.</param>
    /// <param name="args">The state of the beginning of the collision.</param>
    private void HandleParticleCollide(EntityUid uid, ParticleProjectileComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<SingularityGeneratorComponent>(args.OtherEntity, out var generatorComp))
            return;

        if (_timing.CurTime < _metadata.GetPauseTime(uid) + generatorComp.NextFailsafe && !generatorComp.FailsafeDisabled)
        {
            QueueDel(uid);
            return;
        }

        var contained = true;
        if (!generatorComp.FailsafeDisabled)
        {
            var transform = Transform(args.OtherEntity);
            var directions = Enum.GetValues<Direction>().Length;
            for (var i = 0; i < directions - 1; i += 2) // Skip every other direction, checking only cardinals
            {
                if (!CheckContainmentField((Direction)i, new Entity<SingularityGeneratorComponent>(args.OtherEntity, generatorComp), transform))
                    contained = false;
            }
        }

        if (!contained && !generatorComp.FailsafeDisabled)
        {
            generatorComp.NextFailsafe = _timing.CurTime + generatorComp.FailsafeCooldown;
            PopupSystem.PopupEntity(Loc.GetString("comp-generator-failsafe", ("target", args.OtherEntity)), args.OtherEntity, PopupType.LargeCaution);
        }
        else
        {
            SetPower(
                args.OtherEntity,
                generatorComp.Power + component.State switch
                {
                    ParticleAcceleratorPowerState.Standby => 0,
                    ParticleAcceleratorPowerState.Level0 => 1,
                    ParticleAcceleratorPowerState.Level1 => 2,
                    ParticleAcceleratorPowerState.Level2 => 4,
                    ParticleAcceleratorPowerState.Level3 => 8,
                    _ => 0
                },
                generatorComp
            );
        }

        QueueDel(uid);
    }
    #endregion Event Handlers

    /// <summary>
    /// Checks whether there's a containment field in a given direction away from the generator
    /// </summary>
    /// <param name="transform">The transform component of the singularity generator.</param>
    /// <remarks>Mostly copied from <see cref="ContainmentFieldGeneratorSystem"/> </remarks>
    private bool CheckContainmentField(Direction dir, Entity<SingularityGeneratorComponent> generator, TransformComponent transform)
    {
        var component = generator.Comp;

        var (worldPosition, worldRotation) = _transformSystem.GetWorldPositionRotation(transform);
        var dirRad = dir.ToAngle() + worldRotation;

        var ray = new CollisionRay(worldPosition, dirRad.ToVec(), component.CollisionMask);
        var rayCastResults = _physics.IntersectRay(transform.MapID, ray, component.FailsafeDistance, generator, false);
        var genQuery = GetEntityQuery<ContainmentFieldComponent>();

        RayCastResults? closestResult = null;

        foreach (var result in rayCastResults)
        {
            if (!genQuery.HasComponent(result.HitEntity))
                continue;

            closestResult = result;
            break;
        }

        if (closestResult == null)
            return false;

        var ent = closestResult.Value.HitEntity;

        // Check that the field can't be moved. The fields' transform parenting is weird, so skip that
        return TryComp<PhysicsComponent>(ent, out var collidableComponent) && collidableComponent.BodyType == BodyType.Static;
    }
}
