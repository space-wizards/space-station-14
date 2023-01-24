using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

using Content.Shared.Radiation.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.Events;

namespace Content.Shared.Singularity.EntitySystems;

/// <summary>
/// The entity system primarily responsible for managing <see cref="SingularityComponent"/>s.
/// </summary>
public abstract class SharedSingularitySystem : EntitySystem
{
#region Dependencies
    [Dependency] private readonly SharedAppearanceSystem _visualizer = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedEventHorizonSystem _horizons = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly IViewVariablesManager Vvm = default!;
#endregion Dependencies

    /// <summary>
    /// The minimum level a singularity can be set to.
    /// </summary>
    public const byte MinSingularityLevel = 0;

    /// <summary>
    /// The maximum level a singularity can be set to.
    /// </summary>
    public const byte MaxSingularityLevel = 6;

    /// <summary>
    /// The amount to scale a singularities distortion shader by when it's in a container.
    /// This is the inverse of an exponent, not a linear scaling factor.
    /// ie. n => intensity = intensity ** (1/n)
    /// </summary>
    public const float DistortionContainerScaling = 4f;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SingularityComponent, ComponentStartup>(OnSingularityStartup);
        SubscribeLocalEvent<AppearanceComponent, SingularityLevelChangedEvent>(UpdateAppearance);
        SubscribeLocalEvent<RadiationSourceComponent, SingularityLevelChangedEvent>(UpdateRadiation);
        SubscribeLocalEvent<PhysicsComponent, SingularityLevelChangedEvent>(UpdateBody);
        SubscribeLocalEvent<EventHorizonComponent, SingularityLevelChangedEvent>(UpdateEventHorizon);
        SubscribeLocalEvent<SingularityDistortionComponent, SingularityLevelChangedEvent>(UpdateDistortion);
        SubscribeLocalEvent<SingularityDistortionComponent, EntGotInsertedIntoContainerMessage>(UpdateDistortion);
        SubscribeLocalEvent<SingularityDistortionComponent, EntGotRemovedFromContainerMessage>(UpdateDistortion);

        var vvHandle = Vvm.GetTypeHandler<SingularityComponent>();
        vvHandle.AddPath(nameof(SingularityComponent.Level), (_, comp) => comp.Level, SetLevel);
        vvHandle.AddPath(nameof(SingularityComponent.RadsPerLevel), (_, comp) => comp.RadsPerLevel, SetRadsPerLevel);
    }

    public override void Shutdown()
    {
        var vvHandle = Vvm.GetTypeHandler<SingularityComponent>();
        vvHandle.RemovePath(nameof(SingularityComponent.Level));
        vvHandle.RemovePath(nameof(SingularityComponent.RadsPerLevel));

        base.Shutdown();
    }

#region Getters/Setters

    /// <summary>
    /// Setter for <see cref="SingularityComponent.Level"/>
    /// Also sends out an event alerting that the singularities level has changed.
    /// </summary>
    /// <param name="uid">The uid of the singularity to change the level of.</param>
    /// <param name="value">The new level the singularity should have.</param>
    /// <param name="singularity">The state of the singularity to change the level of.</param>
    public void SetLevel(EntityUid uid, byte value, SingularityComponent? singularity = null)
    {
        if(!Resolve(uid, ref singularity))
            return;

        value = MathHelper.Clamp(value, MinSingularityLevel, MaxSingularityLevel);
        var oldValue = singularity.Level;
        if (oldValue == value)
            return;

        singularity.Level = value;
        UpdateSingularityLevel(uid, oldValue, singularity);
        if(!EntityManager.Deleted(singularity.Owner))
            EntityManager.Dirty(singularity);
    }

    /// <summary>
    /// Setter for <see cref="SingularityComponent.RadsPerLevel"/>
    /// Also updates the radiation output of the singularity according to the new values.
    /// </summary>
    /// <param name="uid">The uid of the singularity to change the radioactivity of.</param>
    /// <param name="value">The new radioactivity the singularity should have.</param>
    /// <param name="singularity">The state of the singularity to change the radioactivity of.</param>
    public void SetRadsPerLevel(EntityUid uid, float value, SingularityComponent? singularity = null)
    {
        if(!Resolve(uid, ref singularity))
            return;

        var oldValue = singularity.RadsPerLevel;
        if (oldValue == value)
            return;

        singularity.RadsPerLevel = value;
        UpdateRadiation(uid, singularity);
    }

    /// <summary>
    /// Alerts the entity hosting the singularity that the level of the singularity has changed.
    /// Usually follows a SharedSingularitySystem.SetLevel call, but is also used on component startup to sync everything.
    /// </summary>
    /// <param name="uid">The uid of the singularity which's level has changed.</param>
    /// <param name="oldValue">The old level of the singularity. May be equal to <see cref="SingularityComponent.Level"/> if the component is starting.</param>
    /// <param name="singularity">The state of the singularity which's level has changed.</param>
    public void UpdateSingularityLevel(EntityUid uid, byte oldValue, SingularityComponent? singularity = null)
    {
        if(!Resolve(uid, ref singularity))
            return;

        RaiseLocalEvent(uid, new SingularityLevelChangedEvent(singularity.Level, oldValue, singularity));
        if (singularity.Level <= 0)
            EntityManager.DeleteEntity(singularity.Owner);
    }

    /// <summary>
    /// Alerts the entity hosting the singularity that the level of the singularity has changed without the level actually changing.
    /// Used to sync components when the singularity component is added to an entity.
    /// </summary>
    /// <param name="uid">The uid of the singularity.</param>
    /// <param name="singularity">The state of the singularity.</param>
    public void UpdateSingularityLevel(EntityUid uid, SingularityComponent? singularity = null)
    {
        if (Resolve(uid, ref singularity))
            UpdateSingularityLevel(uid, singularity.Level, singularity);
    }

    /// <summary>
    /// Updates the amount of radiation the singularity emits to reflect a change in the level or radioactivity per level of the singularity.
    /// </summary>
    /// <param name="uid">The uid of the singularity to update the radiation of.</param>
    /// <param name="singularity">The state of the singularity to update the radiation of.</param>
    /// <param name="rads">The state of the radioactivity of the singularity to update.</param>
    private void UpdateRadiation(EntityUid uid, SingularityComponent? singularity = null, RadiationSourceComponent? rads = null)
    {
        if(!Resolve(uid, ref singularity, ref rads, logMissing: false))
            return;
        rads.Intensity = singularity.Level * singularity.RadsPerLevel;
    }

#endregion Getters/Setters

#region Derivations
    /// <summary>
    /// The scaling factor for the size of a singularities gravity well.
    /// </summary>
    public const float BaseGravityWellRadius = 2f;

    /// <summary>
    /// The scaling factor for the base acceleration of a singularities gravity well.
    /// </summary>
    public const float BaseGravityWellAcceleration = 10f;

    /// <summary>
    /// The level at and above which a singularity should be capable of breaching containment.
    /// </summary>
    public const byte SingularityBreachThreshold = 5;

    /// <summary>
    /// Derives the proper gravity well radius for a singularity from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>The gravity well radius the singularity should have given its state.</returns>
    public float GravPulseRange(SingularityComponent singulo)
        => BaseGravityWellRadius * (singulo.Level + 1);

    /// <summary>
    /// Derives the proper base gravitational acceleration for a singularity from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>The base gravitational acceleration the singularity should have given its state.</returns>
    public (float, float) GravPulseAcceleration(SingularityComponent singulo)
        => (BaseGravityWellAcceleration * singulo.Level, 0f);

    /// <summary>
    /// Derives the proper event horizon radius for a singularity from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>The event horizon radius the singularity should have given its state.</returns>
    public float EventHorizonRadius(SingularityComponent singulo)
        => singulo.Level - 0.5f;

    /// <summary>
    /// Derives whether a singularity should be able to breach containment from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>Whether the singularity should be able to breach containment.</returns>
    public bool CanBreachContainment(SingularityComponent singulo)
        => singulo.Level >= SingularityBreachThreshold;

    /// <summary>
    /// Derives the proper distortion shader falloff for a singularity from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>The distortion shader falloff the singularity should have given its state.</returns>
    public float GetFalloff(float level)
    {
        return level switch {
            0 => 9999f,
            1 => MathF.Sqrt(6.4f),
            2 => MathF.Sqrt(7.0f),
            3 => MathF.Sqrt(8.0f),
            4 => MathF.Sqrt(10.0f),
            5 => MathF.Sqrt(12.0f),
            6 => MathF.Sqrt(12.0f),
            _ => -1.0f
        };
    }

    /// <summary>
    /// Derives the proper distortion shader intensity for a singularity from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>The distortion shader intensity the singularity should have given its state.</returns>
    public float GetIntensity(float level)
    {
        return level switch {
            0 => 0.0f,
            1 => 3645f,
            2 => 103680f,
            3 => 1113920f,
            4 => 16200000f,
            5 => 180000000f,
            6 => 180000000f,
            _ => -1.0f
        };
    }
#endregion Derivations

#region Serialization
    /// <summary>
    /// A state wrapper used to sync the singularity between the server and client.
    /// </summary>
    [Serializable, NetSerializable]
    protected sealed class SingularityComponentState : ComponentState
    {
        /// <summary>
        /// The level of the singularity to sync.
        /// </summary>
        public readonly byte Level;

        public SingularityComponentState(SingularityComponent singulo)
        {
            Level = singulo.Level;
        }
    }
#endregion Serialization

#region EventHandlers
    /// <summary>
    /// Syncs other components with the state of the singularity via event on startup.
    /// </summary>
    /// <param name="uid">The entity that is becoming a singularity.</param>
    /// <param name="comp">The singularity component that is being added to the entity.</param>
    /// <param name="args">The event arguments.</param>
    protected virtual void OnSingularityStartup(EntityUid uid, SingularityComponent comp, ComponentStartup args)
    {
        UpdateSingularityLevel(uid, comp);
    }

    // TODO: Figure out which systems should have control of which coupling.
    /// <summary>
    /// Syncs the radius of an event horizon associated with a singularity that just changed levels.
    /// </summary>
    /// <param name="uid">The entity that the event horizon and singularity are attached to.</param>
    /// <param name="comp">The event horizon associated with the singularity.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateEventHorizon(EntityUid uid, EventHorizonComponent comp, SingularityLevelChangedEvent args)
    {
        var singulo = args.Singularity;
        _horizons.SetRadius(uid, EventHorizonRadius(singulo), false, comp);
        _horizons.SetCanBreachContainment(uid, CanBreachContainment(singulo), false, comp);
        _horizons.UpdateEventHorizonFixture(uid, eventHorizon: comp);
    }

    /// <summary>
    /// Updates the distortion shader associated with a singularity when the singuarity changes levels.
    /// </summary>
    /// <param name="uid">The uid of the distortion shader.</param>
    /// <param name="comp">The state of the distortion shader.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateDistortion(EntityUid uid, SingularityDistortionComponent comp, SingularityLevelChangedEvent args)
    {
        var newFalloffPower = GetFalloff(args.NewValue);
        var newIntensity = GetIntensity(args.NewValue);
        if (_containers.IsEntityInContainer(uid))
        {
            var absFalloffPower = MathF.Abs(newFalloffPower);
            var absIntensity = MathF.Abs(newIntensity);

            var factor = (1f / DistortionContainerScaling) - 1f;
            newFalloffPower = absFalloffPower > 1f ? newFalloffPower * MathF.Pow(absFalloffPower, factor) : newFalloffPower;
            newIntensity = absIntensity > 1f ? newIntensity * MathF.Pow(absIntensity, factor) : newIntensity;
        }

        comp.FalloffPower = newFalloffPower;
        comp.Intensity = newIntensity;
    }

    /// <summary>
    /// Updates the distortion shader associated with a singularity when the singuarity is inserted into a container.
    /// </summary>
    /// <param name="uid">The uid of the distortion shader.</param>
    /// <param name="comp">The state of the distortion shader.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateDistortion(EntityUid uid, SingularityDistortionComponent comp, EntGotInsertedIntoContainerMessage args)
    {
        var absFalloffPower = MathF.Abs(comp.FalloffPower);
        var absIntensity = MathF.Abs(comp.Intensity);

        var factor = (1f / DistortionContainerScaling) - 1f;
        comp.FalloffPower = absFalloffPower > 1 ? comp.FalloffPower * MathF.Pow(absFalloffPower, factor) : comp.FalloffPower;
        comp.Intensity = absIntensity > 1 ? comp.Intensity * MathF.Pow(absIntensity, factor) : comp.Intensity;
    }

    /// <summary>
    /// Updates the distortion shader associated with a singularity when the singuarity is removed from a container.
    /// </summary>
    /// <param name="uid">The uid of the distortion shader.</param>
    /// <param name="comp">The state of the distortion shader.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateDistortion(EntityUid uid, SingularityDistortionComponent comp, EntGotRemovedFromContainerMessage args)
    {
        var absFalloffPower = MathF.Abs(comp.FalloffPower);
        var absIntensity = MathF.Abs(comp.Intensity);

        var factor = DistortionContainerScaling - 1;
        comp.FalloffPower = absFalloffPower > 1 ? comp.FalloffPower * MathF.Pow(absFalloffPower, factor) : comp.FalloffPower;
        comp.Intensity = absIntensity > 1 ? comp.Intensity * MathF.Pow(absIntensity, factor) : comp.Intensity;
    }

    /// <summary>
    /// Updates the state of the physics body associated with a singularity when the singualrity changes levels.
    /// </summary>
    /// <param name="uid">The entity that the physics body and singularity are attached to.</param>
    /// <param name="comp">The physics body associated with the singularity.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateBody(EntityUid uid, PhysicsComponent comp, SingularityLevelChangedEvent args)
    {
        _physics.SetBodyStatus(comp, (args.NewValue > 1) ? BodyStatus.InAir : BodyStatus.OnGround);
        if (args.NewValue <= 1 && args.OldValue > 1) // Apparently keeps singularities from getting stuck in the corners of containment fields.
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: comp); // No idea how stopping the singularities movement keeps it from getting stuck though.
    }

    /// <summary>
    /// Updates the appearance of a singularity when the singularities level changes.
    /// </summary>
    /// <param name="uid">The entity that the singularity is attached to.</param>
    /// <param name="comp">The appearance associated with the singularity.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateAppearance(EntityUid uid, AppearanceComponent comp, SingularityLevelChangedEvent args)
    {
        _visualizer.SetData(uid, SingularityVisuals.Level, args.NewValue, comp);
    }

    /// <summary>
    /// Updates the amount of radiation a singularity emits when the singularities level changes.
    /// </summary>
    /// <param name="uid">The entity that the singularity is attached to.</param>
    /// <param name="comp">The radiation source associated with the singularity.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateRadiation(EntityUid uid, RadiationSourceComponent comp, SingularityLevelChangedEvent args)
    {
        UpdateRadiation(uid, args.Singularity, comp);
    }

#endregion EventHandlers

}
