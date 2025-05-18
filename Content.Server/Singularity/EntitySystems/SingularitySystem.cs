using Content.Server.Physics.Components;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.Events;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Singularity.Events;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;

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
    [Dependency] private readonly IGameTiming _timing = default!;
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
        vvHandle.AddPath(nameof(SingularityComponent.Energy), (_, comp) => comp.Energy, SetEnergy);
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
        if(!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<SingularityComponent>();
        while (query.MoveNext(out var uid, out var singularity))
        {
            AdjustEnergy(uid, -singularity.EnergyDrain * frameTime, singularity: singularity);
        }
    }

#region Getters/Setters

    /// <summary>
    /// Setter for <see cref="SingularityComponent.Energy"/>.
    /// Also updates the level of the singularity accordingly.
    /// </summary>
    /// <param name="uid">The uid of the singularity to set the energy of.</param>
    /// <param name="value">The amount of energy for the singularity to have.</param>
    /// <param name="singularity">The state of the singularity to set the energy of.</param>
    public void SetEnergy(EntityUid uid, float value, SingularityComponent? singularity = null)
    {
        if(!Resolve(uid, ref singularity))
            return;

        var oldValue = singularity.Energy;
        if (oldValue == value)
            return;

        singularity.Energy = value;
        SetLevel(uid, value switch
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
        }, singularity);
    }

    /// <summary>
    /// Adjusts the amount of energy the singularity has accumulated.
    /// </summary>
    /// <param name="uid">The uid of the singularity to adjust the energy of.</param>
    /// <param name="delta">The amount to adjust the energy of the singuarity.</param>
    /// <param name="min">The minimum amount of energy for the singularity to be adjusted to.</param>
    /// <param name="max">The maximum amount of energy for the singularity to be adjusted to.</param>
    /// <param name="snapMin">Whether the amount of energy in the singularity should be forced to within the specified range if it already is below it.</param>
    /// <param name="snapMax">Whether the amount of energy in the singularity should be forced to within the specified range if it already is above it.</param>
    /// <param name="singularity">The state of the singularity to adjust the energy of.</param>
    public void AdjustEnergy(EntityUid uid, float delta, float min = float.MinValue, float max = float.MaxValue, bool snapMin = true, bool snapMax = true, SingularityComponent? singularity = null)
    {
        if(!Resolve(uid, ref singularity))
            return;

        var newValue = singularity.Energy + delta;
        if((!snapMin && newValue < min)
        || (!snapMax && newValue > max))
            return;
        SetEnergy(uid, MathHelper.Clamp(newValue, min, max), singularity);
    }


#endregion Getters/Setters

#region Event Handlers

    /// <summary>
    /// Handles playing the startup sounds when a singulo forms.
    /// Always sets up the ambient singularity rumble.
    /// The formation sound only plays if the singularity is being created.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is forming.</param>
    /// <param name="comp">The component of the singularity that is forming.</param>
    /// <param name="args">The event arguments.</param>
    protected override void OnSingularityStartup(EntityUid uid, SingularityComponent comp, ComponentStartup args)
    {
        MetaDataComponent? metaData = null;
        if (Resolve(uid, ref metaData) && metaData.EntityLifeStage <= EntityLifeStage.Initializing)
            _audio.PlayPvs(comp.FormationSound, uid);

        comp.AmbientSoundStream = _audio.PlayPvs(comp.AmbientSound, uid)?.Entity;
        UpdateSingularityLevel(uid, comp);
    }

    /// <summary>
    /// Makes entities that have the singularity distortion visual warping always get their state shared with the client.
    /// This prevents some major popin with large distortion ranges.
    /// </summary>
    /// <param name="uid">The entity UID of the entity that is gaining the shader.</param>
    /// <param name="comp">The component of the shader that the entity is gaining.</param>
    /// <param name="args">The event arguments.</param>
    public void OnDistortionStartup(EntityUid uid, SingularityDistortionComponent comp, ComponentStartup args)
    {
        _pvs.AddGlobalOverride(uid);
    }

    /// <summary>
    /// Handles playing the shutdown sounds when a singulo dissipates.
    /// Always stops the ambient singularity rumble.
    /// The dissipations sound only plays if the singularity is being destroyed.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is dissipating.</param>
    /// <param name="comp">The component of the singularity that is dissipating.</param>
    /// <param name="args">The event arguments.</param>
    public void OnSingularityShutdown(EntityUid uid, SingularityComponent comp, ComponentShutdown args)
    {
        comp.AmbientSoundStream = _audio.Stop(comp.AmbientSoundStream);

        MetaDataComponent? metaData = null;
        if (Resolve(uid, ref metaData) && metaData.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            var xform = Transform(uid);
            var coordinates = xform.Coordinates;

            // I feel like IsValid should be checking this or something idk.
            if (!TerminatingOrDeleted(coordinates.EntityId))
                _audio.PlayPvs(comp.DissipationSound, coordinates);
        }
    }

    /// <summary>
    /// Handles wrapping the state of a singularity for server-client syncing.
    /// </summary>
    /// <param name="uid">The uid of the singularity that is being synced.</param>
    /// <param name="comp">The state of the singularity that is being synced.</param>
    /// <param name="args">The event arguments.</param>
    private void HandleSingularityState(EntityUid uid, SingularityComponent comp, ref ComponentGetState args)
    {
        args.State = new SingularityComponentState(comp);
    }

    /// <summary>
    /// Adds the energy of any entities that are consumed to the singularity that consumed them.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is consuming the entity.</param>
    /// <param name="comp">The component of the singularity that is consuming the entity.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumedEntity(EntityUid uid, SingularityComponent comp, ref EntityConsumedByEventHorizonEvent args)
    {
        // Don't double count singulo food
        if (HasComp<SinguloFoodComponent>(args.Entity))
            return;

        AdjustEnergy(uid, BaseEntityEnergy, singularity: comp);
    }

    /// <summary>
    /// Adds the energy of any tiles that are consumed to the singularity that consumed them.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is consuming the tiles.</param>
    /// <param name="comp">The component of the singularity that is consuming the tiles.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumedTiles(EntityUid uid, SingularityComponent comp, ref TilesConsumedByEventHorizonEvent args)
    {
        AdjustEnergy(uid, args.Tiles.Count * BaseTileEnergy, singularity: comp);
    }

    /// <summary>
    /// Adds the energy of this singularity to singularities that consume it.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is being consumed.</param>
    /// <param name="comp">The component of the singularity that is being consumed.</param>
    /// <param name="args">The event arguments.</param>
    private void OnConsumed(EntityUid uid, SingularityComponent comp, ref EventHorizonConsumedEntityEvent args)
    {
        // Should be slightly more efficient than checking literally everything we consume for a singularity component and doing the reverse.
        if (EntityManager.TryGetComponent<SingularityComponent>(args.EventHorizonUid, out var singulo))
        {
            AdjustEnergy(args.EventHorizonUid, comp.Energy, singularity: singulo);
            SetEnergy(uid, 0.0f, comp);
        }
    }

    /// <summary>
    /// Adds some bonus energy from any singularity food to the singularity that consumes it.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity food that is being consumed.</param>
    /// <param name="comp">The component of the singularity food that is being consumed.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumed(EntityUid uid, SinguloFoodComponent comp, ref EventHorizonConsumedEntityEvent args)
    {
        if (EntityManager.TryGetComponent<SingularityComponent>(args.EventHorizonUid, out var singulo))
        {
            // Calculate the percentage change (positive or negative)
            var percentageChange = singulo.Energy * (comp.EnergyFactor - 1f);
            // Apply both the flat and percentage changes
            AdjustEnergy(args.EventHorizonUid, comp.Energy + percentageChange, singularity: singulo);
        }
    }

    /// <summary>
    /// Updates the rate at which the singularities energy drains at when its level changes.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that changed in level.</param>
    /// <param name="comp">The component of the singularity that changed in level.</param>
    /// <param name="args">The event arguments.</param>
    public void UpdateEnergyDrain(EntityUid uid, SingularityComponent comp, SingularityLevelChangedEvent args)
    {
        comp.EnergyDrain = args.NewValue switch
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
    private void UpdateRandomWalk(EntityUid uid, RandomWalkComponent comp, SingularityLevelChangedEvent args)
    {
        var scale = MathF.Max(args.NewValue, 4);
        comp.MinSpeed = 7.5f / scale;
        comp.MaxSpeed = 10f / scale;
    }

    /// <summary>
    /// Updates the size and strength of the singularities gravity well when the singularities level changes.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity.</param>
    /// <param name="comp">The gravity well component sharing the entity with the singulo component.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateGravityWell(EntityUid uid, GravityWellComponent comp, SingularityLevelChangedEvent args)
    {
        var singulos = args.Singularity;
        comp.MaxRange = GravPulseRange(singulos);
        (comp.BaseRadialAcceleration, comp.BaseTangentialAcceleration) = GravPulseAcceleration(singulos);
    }

#endregion Event Handlers
}
