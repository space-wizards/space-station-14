using System.Numerics;
using Content.Shared.Radiation.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.Events;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

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
        SubscribeLocalEvent<SingularityDistortionComponent, SingularityLevelChangedEvent>(UpdateDistortion);
        SubscribeLocalEvent<SingularityDistortionComponent, EntGotInsertedIntoContainerMessage>(UpdateDistortion);
        SubscribeLocalEvent<SingularityDistortionComponent, EntGotRemovedFromContainerMessage>(UpdateDistortion);

        var vvHandle = Vvm.GetTypeHandler<SingularityComponent>();
        vvHandle.AddPath(nameof(SingularityComponent.Level), (_, comp) => comp.Level, (uid, value, comp) => SetLevel((uid, comp), value));
        vvHandle.AddPath(nameof(SingularityComponent.RadsPerLevel), (_, comp) => comp.RadsPerLevel, (uid, value, comp) => SetRadsPerLevel((uid, comp), value));
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
    /// <param name="singularity">The singularity to change the level of.</param>
    /// <param name="value">The new level the singularity should have.</param>
    public void SetLevel(Entity<SingularityComponent?> singularity, byte value)
    {
        if (!Resolve(singularity, ref singularity.Comp))
            return;

        value = MathHelper.Clamp(value, MinSingularityLevel, MaxSingularityLevel);

        var oldValue = singularity.Comp.Level;
        if (oldValue == value)
            return;

        singularity.Comp.Level = value;
        UpdateSingularityLevel(singularity, oldValue);

        if (!TerminatingOrDeleted(singularity))
            Dirty(singularity);
    }

    /// <summary>
    /// Setter for <see cref="SingularityComponent.RadsPerLevel"/>
    /// Also updates the radiation output of the singularity according to the new values.
    /// </summary>
    /// <param name="singularity">The singularity to change the radioactivity of.</param>
    /// <param name="value">The new radioactivity the singularity should have.</param>
    public void SetRadsPerLevel(Entity<SingularityComponent?> singularity, float value)
    {
        if (!Resolve(singularity, ref singularity.Comp))
            return;

        var oldValue = singularity.Comp.RadsPerLevel;
        if (oldValue == value)
            return;

        singularity.Comp.RadsPerLevel = value;
        UpdateRadiation(singularity);
    }

    /// <summary>
    /// Alerts the entity hosting the singularity that the level of the singularity has changed.
    /// Usually follows a SharedSingularitySystem.SetLevel call, but is also used on component startup to sync everything.
    /// </summary>
    /// <param name="singularity">The singularity to propagate level updates for.</param>
    /// <param name="oldValue">The old level of the singularity. May be equal to <see cref="SingularityComponent.Level"/> if the component is starting.</param>
    public void UpdateSingularityLevel(Entity<SingularityComponent?> singularity, byte oldValue)
    {
        if (!Resolve(singularity, ref singularity.Comp))
            return;

        if (TryComp<EventHorizonComponent>(singularity, out var eventHorizon))
        {
            _horizons.SetRadius((singularity, eventHorizon), EventHorizonRadius(singularity.Comp), false);
            _horizons.SetCanBreachContainment((singularity, eventHorizon), CanBreachContainment(singularity.Comp), false);
            _horizons.UpdateEventHorizonFixture((singularity, null, eventHorizon));
        }

        if (TryComp<PhysicsComponent>(singularity, out var body))
        {
            if (singularity.Comp.Level <= 1 && oldValue > 1) // Apparently keeps singularities from getting stuck in the corners of containment fields.
                _physics.SetLinearVelocity(singularity, Vector2.Zero, body: body); // No idea how stopping the singularities movement keeps it from getting stuck though.
        }

        if (TryComp<AppearanceComponent>(singularity, out var appearance))
        {
            _visualizer.SetData(singularity, SingularityAppearanceKeys.Singularity, singularity.Comp.Level, appearance);
        }

        if (TryComp<RadiationSourceComponent>(singularity, out var radiationSource))
        {
            UpdateRadiation((singularity, singularity.Comp, radiationSource));
        }

        {
            var ev = new SingularityLevelChangedEvent((singularity, singularity.Comp), oldValue);
            RaiseLocalEvent(singularity, ref ev);
        }

        if (singularity.Comp.Level <= 0)
            QueueDel(singularity);
    }

    /// <summary>
    /// Alerts the entity hosting the singularity that the level of the singularity has changed without the level actually changing.
    /// Used to sync components when the singularity component is added to an entity.
    /// </summary>
    /// <param name="singularity">The singularity.</param>
    public void UpdateSingularityLevel(Entity<SingularityComponent?> singularity)
    {
        if (Resolve(singularity, ref singularity.Comp))
            UpdateSingularityLevel(singularity, singularity.Comp.Level);
    }

    /// <summary>
    /// Updates the amount of radiation the singularity emits to reflect a change in the level or radioactivity per level of the singularity.
    /// </summary>
    /// <param name="singularity">The singularity to update the radiation of.</param>
    private void UpdateRadiation(Entity<SingularityComponent?, RadiationSourceComponent?> singularity)
    {
        var (uid, singulo, rads) = singularity;

        if (!Resolve(uid, ref singulo, ref rads, logMissing: false))
            return;

        rads.Intensity = singulo.Level * singulo.RadsPerLevel;
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
    {
        return BaseGravityWellRadius * (singulo.Level + 1);
    }

    /// <summary>
    /// Derives the proper base gravitational acceleration for a singularity from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>The base gravitational acceleration the singularity should have given its state.</returns>
    public (float, float) GravPulseAcceleration(SingularityComponent singulo)
    {
        return (BaseGravityWellAcceleration * singulo.Level, 0f);
    }

    /// <summary>
    /// Derives the proper event horizon radius for a singularity from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>The event horizon radius the singularity should have given its state.</returns>
    public float EventHorizonRadius(SingularityComponent singulo)
    {
        return singulo.Level - 0.5f;
    }

    /// <summary>
    /// Derives whether a singularity should be able to breach containment from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>Whether the singularity should be able to breach containment.</returns>
    public bool CanBreachContainment(SingularityComponent singulo)
    {
        return singulo.Level >= SingularityBreachThreshold;
    }

    /// <summary>
    /// Derives the proper distortion shader falloff for a singularity from its state.
    /// </summary>
    /// <param name="singulo">A singularity.</param>
    /// <returns>The distortion shader falloff the singularity should have given its state.</returns>
    public float GetFalloff(float level)
    {
        return level switch
        {
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
        return level switch
        {
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
    protected virtual void OnSingularityStartup(Entity<SingularityComponent> singularity, ref ComponentStartup args)
    {
        UpdateSingularityLevel(singularity.AsNullable());
    }

    /// <summary>
    /// Updates the distortion shader associated with a singularity when the singuarity changes levels.
    /// </summary>
    /// <param name="uid">The uid of the distortion shader.</param>
    /// <param name="comp">The state of the distortion shader.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateDistortion(Entity<SingularityDistortionComponent> distortion, ref SingularityLevelChangedEvent args)
    {
        var newFalloffPower = GetFalloff(args.NewValue);
        var newIntensity = GetIntensity(args.NewValue);

        if (_containers.IsEntityInContainer(distortion))
        {
            var absFalloffPower = MathF.Abs(newFalloffPower);
            var absIntensity = MathF.Abs(newIntensity);

            var factor = (1f / DistortionContainerScaling) - 1f;
            newFalloffPower = absFalloffPower > 1f ? newFalloffPower * MathF.Pow(absFalloffPower, factor) : newFalloffPower;
            newIntensity = absIntensity > 1f ? newIntensity * MathF.Pow(absIntensity, factor) : newIntensity;
        }

        distortion.Comp.FalloffPower = newFalloffPower;
        distortion.Comp.Intensity = newIntensity;
        Dirty(distortion);
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

    #endregion EventHandlers

    #region Obsolete API

    /// <inheritdoc cref="SetLevel(Entity{SingularityComponent?}, byte)"/>
    [Obsolete("This method is obsolete, use the Entity<T> overload instead.")]
    public void SetLevel(EntityUid uid, byte value, SingularityComponent? singularity = null)
    {
        SetLevel((uid, singularity), value);
    }

    /// <inheritdoc cref="SetRadsPerLevel(Entity{SingularityComponent?}, float)"/>
    [Obsolete("This method is obsolete, use the Entity<T> overload instead.")]
    public void SetRadsPerLevel(EntityUid uid, float value, SingularityComponent? singularity = null)
    {
        SetRadsPerLevel((uid, singularity), value);
    }

    /// <inheritdoc cref="UpdateSingularityLevel(Entity{SingularityComponent?}, byte)"/>
    [Obsolete("This method is obsolete, use the Entity<T> overload instead.")]
    public void UpdateSingularityLevel(EntityUid uid, byte oldValue, SingularityComponent? singularity = null)
    {
        UpdateSingularityLevel((uid, singularity), oldValue);
    }

    /// <inheritdoc cref="UpdateSingularityLevel(Entity{SingularityComponent?})"/>
    [Obsolete("This method is obsolete, use the Entity<T> overload instead.")]
    public void UpdateSingularityLevel(EntityUid uid, SingularityComponent? singularity = null)
    {
        UpdateSingularityLevel((uid, singularity));
    }

    #endregion Obsolete API
}
