using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Server.GameStates;

using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Singularity.Events;

using Content.Server.Physics.Components;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.Events;

namespace Content.Server.Singularity.EntitySystems;

/// <summary>
/// The server-side version of <seed cref="SharedSingularitySystem">.
/// Primarily responsible for managing <see cref="SingularityComponent"/>s.
/// Handles their accumulation of energy upon consuming entities (see <see cref="EventHorizonComponent">) and gradual dissipation.
/// Also handles synchronizing server-side components with the singuarities level.
/// </summary>
public sealed class SingularitySystem : SharedSingularitySystem
{
#region Dependencies
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PVSOverrideSystem _pvs = default!;
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
        SubscribeLocalEvent<SingularityComponent, ComponentStartup>(OnSingularityStartup);
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

        var vvHandle = _vvm.GetTypeHandler<SingularityComponent>();
        vvHandle.AddPath(nameof(SingularityComponent.Energy), (_, comp) => comp.Energy, SetEnergy);
    }

    public override void Shutdown()
    {
        _vvm.GetTypeHandler<SingularityComponent>()
            .RemovePath(nameof(SingularityComponent.Energy));

        base.Shutdown();
    }

    /// <summary>
    /// Handles the gradual dissipation of all singularities.
    /// </summary>
    /// <param name="frameTime">The amount of time since the last set of updates.</param>
    public override void Update(float frameTime)
    {
        foreach(var singularity in EntityManager.EntityQuery<SingularityComponent>())
        {
            if ((singularity.TimeSinceLastUpdate += frameTime) > singularity.UpdatePeriod)
                Update(singularity, singularity.TimeSinceLastUpdate);
        }
    }

    /// <summary>
    /// Handles the gradual energy loss and dissipation of a singularity.
    /// </summary>
    /// <param name="singularity">The singularity to update the energy of.</param>
    /// <param name="frameTime">The amount of time to consider as having passed since the last update.</param>
    public void Update(SingularityComponent singularity, float frameTime)
    {
        singularity.TimeSinceLastUpdate = 0.0f;
        AdjustEnergy(singularity, -singularity.EnergyDrain * frameTime);
    }

    /// <summary>
    /// Handles the gradual energy loss and dissipation of singularity.
    /// </summary>
    /// <param name="singularity">The singularity to update the energy of</param>
    public void Update(SingularityComponent singularity)
        => Update(singularity, singularity.TimeSinceLastUpdate);

#region Getters/Setters
    /// <summary>
    /// Setter for <see cref="SingularityComponent.Energy"/>.
    /// Also updates the level of the singularity accordingly.
    /// </summary>
    /// <param name="singularity">The singularity to set the energy of.</param>
    /// <param name="value">The amount of energy for the singularity to have.</param>
    public void SetEnergy(SingularityComponent singularity, float value)
    {
        var oldValue = singularity.Energy;
        if (oldValue == value)
            return;

        singularity.Energy = value;
        SetLevel(singularity, value switch {
                >= 1500 => 6,
                >= 1000 => 5,
                >= 600 => 4,
                >= 300 => 3,
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
    /// <param name="hardMin">Whether the amount of energy in the singularity should be forced to within the specified range if it already is below it.</param>
    /// <param name="hardMax">Whether the amount of energy in the singularity should be forced to within the specified range if it already is above it.</param>
    public void AdjustEnergy(SingularityComponent singularity, float delta, float min = float.MinValue, float max = float.MaxValue, bool snapMin = true, bool snapMax = true)
    {
        var newValue = singularity.Energy + delta;
        if((!snapMin && newValue < min)
        || (!snapMax && newValue > max))
            return;
        SetEnergy(singularity, MathHelper.Clamp(newValue, min, max));
    }
#region VV
    /// <summary>
    /// VV Setter for <see cref="SingularityComponent.Energy"/>.
    /// Also updates the level of the singularity accordingly.
    /// </summary>
    /// <param name="uid">The entity hosting the singularity that is being modified.</param>
    /// <param name="value">The amount of energy for the singularity to have.</param>
    /// <param name="comp">The singularity to set the energy of.</param>
    private void SetEnergy(EntityUid uid, float value, SingularityComponent? comp)
    {
        if (Resolve(uid, ref comp))
            SetEnergy(comp, value);
    }
#endregion VV
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
    public void OnSingularityStartup(EntityUid uid, SingularityComponent comp, ComponentStartup args)
    {
        MetaDataComponent? metaData = null;
        if (Resolve(uid, ref metaData) && metaData.EntityLifeStage <= EntityLifeStage.Initializing)
            _audio.Play(comp.FormationSound, Filter.Pvs(comp.Owner), comp.Owner, true);

        comp.AmbientSoundStream = _audio.Play(comp.AmbientSound, Filter.Pvs(comp.Owner), comp.Owner, true);
        UpdateSingularityLevel(comp);
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
        comp.AmbientSoundStream?.Stop();

        MetaDataComponent? metaData = null;
        if (Resolve(uid, ref metaData) && metaData.EntityLifeStage >= EntityLifeStage.Terminating)
            _audio.Play(comp.DissipationSound, Filter.Pvs(comp.Owner), comp.Owner, true);
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
    public void OnConsumedEntity(EntityUid uid, SingularityComponent comp, EntityConsumedByEventHorizonEvent args)
    {
        AdjustEnergy(comp, BaseEntityEnergy);
    }

    /// <summary>
    /// Adds the energy of any tiles that are consumed to the singularity that consumed them.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is consuming the tiles.</param>
    /// <param name="comp">The component of the singularity that is consuming the tiles.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumedTiles(EntityUid uid, SingularityComponent comp, TilesConsumedByEventHorizonEvent args)
    {
        AdjustEnergy(comp, args.Tiles.Count * BaseTileEnergy);
    }

    /// <summary>
    /// Adds the energy of this singularity to singularities consume it.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is being consumed.</param>
    /// <param name="comp">The component of the singularity that is being consumed.</param>
    /// <param name="args">The event arguments.</param>
    private void OnConsumed(EntityUid uid, SingularityComponent comp, EventHorizonConsumedEntityEvent args)
    {
        // Should be slightly more efficient than checking literally everything we consume for a singularity component and doing the reverse.
        if (EntityManager.TryGetComponent<SingularityComponent>(args.EventHorizon.Owner, out var singulo))
        {
            AdjustEnergy(singulo, comp.Energy);
            SetEnergy(comp, 0.0f);
        }
    }

    /// <summary>
    /// Adds some bonus energy from any singularity food to the singularity that consumes it.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity food that is being consumed.</param>
    /// <param name="comp">The component of the singularity food that is being consumed.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumed(EntityUid uid, SinguloFoodComponent comp, EventHorizonConsumedEntityEvent args)
    {
        if (EntityManager.TryGetComponent<SingularityComponent>(args.EventHorizon.Owner, out var singulo))
            AdjustEnergy(singulo, comp.Energy);
    }

    /// <summary>
    /// Updates the rate at which the singularities energy drains at when its level changes.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that changed in level.</param>
    /// <param name="comp">The component of the singularity that changed in level.</param>
    /// <param name="args">The event arguments.</param>
    public void UpdateEnergyDrain(EntityUid uid, SingularityComponent comp, SingularityLevelChangedEvent args)
    {
        comp.EnergyDrain = args.NewValue switch {
            6 => 20,
            5 => 15,
            4 => 10,
            3 => 5,
            2 => 2,
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
