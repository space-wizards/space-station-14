using Content.Server.Physics.Components;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.Events;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Singularity.Events;
using Robust.Server.GameStates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;

namespace Content.Server.Singularity.EntitySystems;

/// <summary>
/// The server-side version of <see cref="SharedSingularitySystem"/>.
/// Primarily responsible for managing <see cref="SingularityComponent"/>s.
/// Handles their accumulation of energy upon consuming entities (see <see cref="EventHorizonComponent"/>) and gradual dissipation.
/// Also handles synchronizing server-side components with the singuarities level.
/// </summary>
public sealed class SingularitySystem : SharedSingularitySystem
{
#region Dependencies
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    #endregion Dependencies

    /// <summary>
    /// The amount of energy singulos accumulate when they eat a tile.
    /// </summary>
    public const float BaseTileEnergy = 1f;

    /// <summary>
    /// The amount of energy singulos accumulate when they eat an entity.
    /// </summary>
    public const float BaseEntityEnergy = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SingularityDistortionComponent, ComponentStartup>(OnDistortionStartup);
        SubscribeLocalEvent<SingularityComponent, ComponentShutdown>(OnSingularityShutdown);
        SubscribeLocalEvent<SingularityComponent, EventHorizonConsumedEntityEvent>(OnConsumed);
        SubscribeLocalEvent<SinguloFoodComponent, EventHorizonConsumedEntityEvent>(OnConsumed);
        SubscribeLocalEvent<SingularityComponent, EntityConsumedByEventHorizonEvent>(OnConsumedEntity);
        SubscribeLocalEvent<SingularityComponent, TilesConsumedByEventHorizonEvent>(OnConsumedTiles);
        SubscribeLocalEvent<SingularityComponent, SingularityLevelChangedEvent>(UpdateEnergyDrain);
        SubscribeLocalEvent<SingularityComponent, ComponentGetState>(HandleSingularityState);

        // TODO: Figure out where all this coupling should be handled.
        SubscribeLocalEvent<RandomWalkComponent, SingularityLevelChangedEvent>(UpdateRandomWalk);
        SubscribeLocalEvent<GravityWellComponent, SingularityLevelChangedEvent>(UpdateGravityWell);

        var vvHandle = Vvm.GetTypeHandler<SingularityComponent>();
        vvHandle.AddPath(nameof(SingularityComponent.Energy), (_, comp) => comp.Energy, (uid, value, comp) => SetEnergy((uid, comp), value));
    }

    public override void Shutdown()
    {
        var vvHandle = Vvm.GetTypeHandler<SingularityComponent>();
        vvHandle.RemovePath(nameof(SingularityComponent.Energy));
        base.Shutdown();
    }

    /// <summary>
    /// Handles the gradual dissipation of all singularities.
    /// </summary>
    /// <param name="frameTime">The amount of time since the last set of updates.</param>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SingularityComponent>();
        while (query.MoveNext(out var uid, out var singularity))
        {
            AdjustEnergy((uid, singularity), -singularity.EnergyDrain * frameTime);
        }
    }

    #region Getters/Setters

    /// <summary>
    /// Setter for <see cref="SingularityComponent.Energy"/>.
    /// Also updates the level of the singularity accordingly.
    /// </summary>
    /// <param name="singularity">The singularity to set the energy of.</param>
    /// <param name="value">The amount of energy for the singularity to have.</param>
    public void SetEnergy(Entity<SingularityComponent?> singularity, float value)
    {
        if (!Resolve(singularity, ref singularity.Comp))
            return;

        var oldValue = singularity.Comp.Energy;
        if (oldValue == value)
            return;

        singularity.Comp.Energy = value;

        SetLevel(singularity, value switch
        {
            // Normally, a level 6 singularity requires the supermatter + 3000 energy.
            // The required amount of energy has been bumped up to compensate for the lack of the supermatter.
            >= 5000 => 6,
            >= 2000 => 5,
            >= 1000 => 4,
            >= 500 => 3,
            >= 200 => 2,
            > 0 => 1,
            _ => 0
        });
    }

    /// <summary>
    /// Adjusts the amount of energy the singularity has accumulated.
    /// </summary>
    /// <param name="singularity">The singularity to adjust the energy of.</param>
    /// <param name="delta">The amount to adjust the energy of the singuarity.</param>
    /// <param name="min">The minimum amount of energy for the singularity to be adjusted to.</param>
    /// <param name="max">The maximum amount of energy for the singularity to be adjusted to.</param>
    /// <param name="snapMin">Whether the amount of energy in the singularity should be forced to within the specified range if it already is below it.</param>
    /// <param name="snapMax">Whether the amount of energy in the singularity should be forced to within the specified range if it already is above it.</param>
    public void AdjustEnergy(Entity<SingularityComponent?> singularity, float delta, float min = float.MinValue, float max = float.MaxValue, bool snapMin = true, bool snapMax = true)
    {
        if (!Resolve(singularity, ref singularity.Comp))
            return;

        var oldValue = singularity.Comp.Energy;

        if (!snapMin && oldValue < min)
            min = oldValue;
        if (!snapMax && oldValue > max)
            max = oldValue;

        SetEnergy(singularity, MathHelper.Clamp(oldValue + delta, min, max));
    }

    #endregion Getters/Setters

    #region Event Handlers

    /// <summary>
    /// Handles playing the startup sounds when a singulo forms.
    /// Always sets up the ambient singularity rumble.
    /// The formation sound only plays if the singularity is being created.
    /// </summary>
    /// <param name="singularity">The singularity that is forming.</param>
    /// <param name="args">The event arguments.</param>
    protected override void OnSingularityStartup(Entity<SingularityComponent> singularity, ref ComponentStartup args)
    {
        if (TryComp(singularity, out MetaDataComponent? metaData) && metaData.EntityLifeStage <= EntityLifeStage.Initializing)
            _audio.PlayPvs(singularity.Comp.FormationSound, singularity);

        singularity.Comp.AmbientSoundStream = _audio.PlayPvs(singularity.Comp.AmbientSound, singularity)?.Entity;
        UpdateSingularityLevel(singularity.AsNullable());
    }

    /// <summary>
    /// Makes entities that have the singularity distortion visual warping always get their state shared with the client.
    /// This prevents some major popin with large distortion ranges.
    /// </summary>
    /// <param name="distortion">The entity that is gaining the shader.</param>
    /// <param name="args">The event arguments.</param>
    public void OnDistortionStartup(Entity<SingularityDistortionComponent> distortion, ref ComponentStartup args)
    {
        _pvs.AddGlobalOverride(distortion);
    }

    /// <summary>
    /// Handles playing the shutdown sounds when a singulo dissipates.
    /// Always stops the ambient singularity rumble.
    /// The dissipations sound only plays if the singularity is being destroyed.
    /// </summary>
    /// <param name="singularity">The singularity that is dissipating.</param>
    /// <param name="comp">The component of the singularity that is dissipating.</param>
    /// <param name="args">The event arguments.</param>
    public void OnSingularityShutdown(Entity<SingularityComponent> singularity, ref ComponentShutdown args)
    {
        singularity.Comp.AmbientSoundStream = _audio.Stop(singularity.Comp.AmbientSoundStream);

        if (TryComp(singularity, out MetaDataComponent? metaData) && metaData.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            var xform = Transform(singularity);
            var coordinates = xform.Coordinates;

            // I feel like IsValid should be checking this or something idk.
            if (!TerminatingOrDeleted(coordinates.EntityId))
                _audio.PlayPvs(singularity.Comp.DissipationSound, coordinates);
        }
    }

    /// <summary>
    /// Handles wrapping the state of a singularity for server-client syncing.
    /// </summary>
    /// <param name="uid">The uid of the singularity that is being synced.</param>
    /// <param name="comp">The state of the singularity that is being synced.</param>
    /// <param name="args">The event arguments.</param>
    private void HandleSingularityState(Entity<SingularityComponent> singularity, ref ComponentGetState args)
    {
        args.State = new SingularityComponentState(singularity.Comp);
    }

    /// <summary>
    /// Adds the energy of any entities that are consumed to the singularity that consumed them.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is consuming the entity.</param>
    /// <param name="comp">The component of the singularity that is consuming the entity.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumedEntity(Entity<SingularityComponent> singularity, ref EntityConsumedByEventHorizonEvent args)
    {
        // Don't double count singulo food
        if (HasComp<SinguloFoodComponent>(args.Entity))
            return;

        AdjustEnergy(singularity.AsNullable(), BaseEntityEnergy);
    }

    /// <summary>
    /// Adds the energy of any tiles that are consumed to the singularity that consumed them.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is consuming the tiles.</param>
    /// <param name="comp">The component of the singularity that is consuming the tiles.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumedTiles(Entity<SingularityComponent> singularity, ref TilesConsumedByEventHorizonEvent args)
    {
        AdjustEnergy(singularity.AsNullable(), args.Tiles.Count * BaseTileEnergy);
    }

    /// <summary>
    /// Adds the energy of this singularity to singularities that consume it.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is being consumed.</param>
    /// <param name="comp">The component of the singularity that is being consumed.</param>
    /// <param name="args">The event arguments.</param>
    private void OnConsumed(Entity<SingularityComponent> singularity, ref EventHorizonConsumedEntityEvent args)
    {
        // Should be slightly more efficient than checking literally everything we consume for a singularity component and doing the reverse.
        if (TryComp<SingularityComponent>(args.EventHorizon, out var singulo))
        {
            AdjustEnergy((args.EventHorizon, singulo), singularity.Comp.Energy);
            SetEnergy(singularity.AsNullable(), 0.0f);
        }
    }

    /// <summary>
    /// Adds some bonus energy from any singularity food to the singularity that consumes it.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity food that is being consumed.</param>
    /// <param name="comp">The component of the singularity food that is being consumed.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumed(Entity<SinguloFoodComponent> morsel, ref EventHorizonConsumedEntityEvent args)
    {
        if (TryComp<SingularityComponent>(args.EventHorizon, out var singulo))
        {
            // Calculate the percentage change (positive or negative)
            var percentageChange = singulo.Energy * (morsel.Comp.EnergyFactor - 1f);
            // Apply both the flat and percentage changes
            AdjustEnergy((args.EventHorizon, singulo), morsel.Comp.Energy + percentageChange);
        }
    }

    /// <summary>
    /// Updates the rate at which the singularities energy drains at when its level changes.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that changed in level.</param>
    /// <param name="comp">The component of the singularity that changed in level.</param>
    /// <param name="args">The event arguments.</param>
    public void UpdateEnergyDrain(Entity<SingularityComponent> singularity, ref SingularityLevelChangedEvent args)
    {
        singularity.Comp.EnergyDrain = args.NewValue switch
        {
            6 => 0,
            5 => 0,
            4 => 20,
            3 => 10,
            2 => 5,
            1 => 1,
            _ => 0
        };
    }

    /// <summary>
    /// Updates the possible speeds of the singulos random walk when the singularities level changes.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity.</param>
    /// <param name="comp">The random walk component component sharing the entity with the singulo component.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateRandomWalk(Entity<RandomWalkComponent> drunkard, ref SingularityLevelChangedEvent args)
    {
        var scale = MathF.Max(args.NewValue, 4);

        drunkard.Comp.MinSpeed = 7.5f / scale;
        drunkard.Comp.MaxSpeed = 10f / scale;
    }

    /// <summary>
    /// Updates the size and strength of the singularities gravity well when the singularities level changes.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity.</param>
    /// <param name="comp">The gravity well component sharing the entity with the singulo component.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateGravityWell(Entity<GravityWellComponent> gravityWell, ref SingularityLevelChangedEvent args)
    {
        var singulos = args.Singularity;

        gravityWell.Comp.MaxRange = GravPulseRange(singulos);
        (gravityWell.Comp.BaseRadialAcceleration, gravityWell.Comp.BaseTangentialAcceleration) = GravPulseAcceleration(singulos);
    }

    #endregion Event Handlers

    #region Obsolete API

    /// <inheritdoc cref="SetEnergy(Entity{SingularityComponent?}, float)"/>
    [Obsolete("This method is obsolete, use the Entity<T> overload instead.")]
    public void SetEnergy(EntityUid uid, float value, SingularityComponent? singularity = null)
    {
        SetEnergy((uid, singularity), value);
    }

    /// <inheritdoc cref="AdjustEnergy(Entity{SingularityComponent?}, float, float, float, bool, bool)"/>
    [Obsolete("This method is obsolete, use the Entity<T> overload instead.")]
    public void AdjustEnergy(EntityUid uid, float delta, float min = float.MinValue, float max = float.MaxValue, bool snapMin = true, bool snapMax = true, SingularityComponent? singularity = null)
    {
        AdjustEnergy((uid, singularity), delta, min, max, snapMin, snapMax);
    }

    #endregion Obsolete API
}
