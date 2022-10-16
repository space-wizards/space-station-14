using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Singularity.Events;

using Content.Server.Physics.Components;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.Events;

namespace Content.Server.Singularity.EntitySystems;

public sealed class SingularitySystem : SharedSingularitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <summary>
    ///     The amount of energy singulos accumulate when they eat a tile.
    /// </summary>
    public const float BaseTileEnergy = 1f;

    /// <summary>
    ///     The amount of energy singulos accumulate when they eat an entity.
    /// </summary>
    public const float BaseEntityEnergy = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SingularityComponent, ComponentStartup>(OnSingularityStartup);
        SubscribeLocalEvent<SingularityComponent, ComponentShutdown>(OnSingularityShutdown);
        SubscribeLocalEvent<SingularityComponent, EventHorizonConsumedEntityEvent>(OnConsumed);
        SubscribeLocalEvent<SinguloFoodComponent, EventHorizonConsumedEntityEvent>(OnConsumed);
        SubscribeLocalEvent<SingularityComponent, EntityConsumedByEventHorizonEvent>(OnConsumedEntity);
        SubscribeLocalEvent<SingularityComponent, TilesConsumedByEventHorizonEvent>(OnConsumedTiles);
        SubscribeLocalEvent<SingularityComponent, SingularityLevelChangedEvent>(UpdateEnergyDrain);
        SubscribeLocalEvent<PhysicsComponent, SingularityLevelChangedEvent>(UpdateBodyStatus);
        SubscribeLocalEvent<RandomWalkComponent, SingularityLevelChangedEvent>(UpdateRandomWalk);
        SubscribeLocalEvent<GravityWellComponent, SingularityLevelChangedEvent>(UpdateGravityWell);
    }

    /// <summary>
    /// Updates the amount of energy in all singularities.
    /// Handles the gradual dissipation of singularities.
    /// </summary>
    /// <param name="frameTime">The amount of time since the last set of updates.</param>
    public override void Update(float frameTime)
    {
        foreach(var singularity in EntityManager.EntityQuery<SingularityComponent>())
        {
            if ((singularity._timeSinceLastUpdate += frameTime) < singularity.UpdatePeriod)
                Update(singularity, singularity._timeSinceLastUpdate);
        }
    }

    /// <summary>
    /// Updates the amount of energy in a singularity.
    /// </summary>
    /// <param name="singularity">The singularity to adjust the energy of.</param>
    /// <param name="frameTime">The amount of time to consider as having passed since the last update.</param>
    public void Update(SingularityComponent singularity, float frameTime)
    {
        singularity._timeSinceLastUpdate = 0.0f;
        SetSingularityEnergy(singularity, singularity.Energy - (singularity.EnergyDrain * frameTime));
    }

    /// <summary>
    /// Updates the amount of energy in a singularity.
    /// </summary>
    /// <param name="singularity">The singularity to adjust the energy of.</param>
    public void Update(SingularityComponent singularity)
        => Update(singularity, singularity._timeSinceLastUpdate);

    /// <summary>
    /// Sets the amount of energy the singularity contains.
    /// </summary>
    /// <param name="singularity"></param>
    /// <param name="value"></param>
    public void SetSingularityEnergy(SingularityComponent singularity, float value)
    {
        var oldValue = singularity._energy;
        if (oldValue == value)
            return;

        singularity._energy = value;
        SetSingularityLevel(singularity, (ulong)value switch {
                >= 1500 => 6,
                >= 1000 => 5,
                >= 600 => 4,
                >= 300 => 3,
                >= 200 => 2,
                < 200 => 1
        });
    }

#region Event Handlers

    /// <summary>
    /// Handles playing the startup sounds when a singulo forms.
    /// Always sets up the ambiant singularity rumble.
    /// The formation sound only plays if the singularity is being created.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is forming.</param>
    /// <param name="comp">The component of the singularity that is forming.</param>
    /// <param name="args">The event arguments.</param>
    public void OnSingularityStartup(EntityUid uid, SingularityComponent comp, ComponentStartup args)
    {
        MetaDataComponent? metaData = null;
        if (Resolve(uid, ref metaData) && metaData.EntityLifeStage <= EntityLifeStage.Initializing)
            _audio.Play(comp.FormationSound, Filter.Pvs(comp.Owner), comp.Owner);

        comp.AmbiantSoundStream = _audio.Play(comp.AmbiantSound, Filter.Pvs(comp.Owner), comp.Owner);
        UpdateSingularityLevel(comp);
    }

    /// <summary>
    /// Handles playing the shutdown sounds when a singulo dissipates.
    /// Always stops the ambiant singularity rumble.
    /// The dissipations sound only plays if the singularity is being destroyed.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is dissipating.</param>
    /// <param name="comp">The component of the singularity that is dissipating.</param>
    /// <param name="args">The event arguments.</param>
    public void OnSingularityShutdown(EntityUid uid, SingularityComponent comp, ComponentShutdown args)
    {
        MetaDataComponent? metaData = null;
        if (Resolve(uid, ref metaData) && metaData.EntityLifeStage >= EntityLifeStage.Terminating)
            _audio.Play(comp.DissipationSound, Filter.Pvs(comp.Owner), comp.Owner);

        comp.AmbiantSoundStream?.Stop();
    }

    /// <summary>
    /// Adds the energy of this singularity to singularities it is consumed by.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is being consumed.</param>
    /// <param name="comp">The component of the singularity that is being consumed.</param>
    /// <param name="args">The event arguments.</param>
    private void OnConsumed(EntityUid uid, SingularityComponent comp, EventHorizonConsumedEntityEvent args)
    {
        if (EntityManager.TryGetComponent<SingularityComponent>(args.EventHorizon.Owner, out var singulo))
        {
            SetSingularityEnergy(singulo, singulo.Energy + comp.Energy);
            SetSingularityEnergy(comp, 0.0f);
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
            SetSingularityEnergy(singulo, singulo.Energy + comp.Energy);
    }

    /// <summary>
    /// Adds the energy of any entities that are consumed to the singularity that consumed them.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is consuming the entity.</param>
    /// <param name="comp">The component of the singularity that is consuming the entity.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumedEntity(EntityUid uid, SingularityComponent comp, EntityConsumedByEventHorizonEvent args)
    {
        SetSingularityEnergy(comp, comp.Energy + BaseEntityEnergy);
    }

    /// <summary>
    /// Adds the energy of any tiles that are consumed to the singularity that consumed them.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity that is consuming the tiles.</param>
    /// <param name="comp">The component of the singularity that is consuming the tiles.</param>
    /// <param name="args">The event arguments.</param>
    public void OnConsumedTiles(EntityUid uid, SingularityComponent comp, TilesConsumedByEventHorizonEvent args)
    {
        SetSingularityEnergy(comp, comp.Energy + BaseTileEnergy * args.Tiles.Count);
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
    /// Updates the status of the physicsbody according to the singulos level.
    /// </summary>
    /// <param name="uid">The entity UID of the singularity.</param>
    /// <param name="comp">The physics component sharing the entity with the singulo component.</param>
    /// <param name="args">The event arguments.</param>
    private void UpdateBodyStatus(EntityUid uid, PhysicsComponent comp, SingularityLevelChangedEvent args)
    {
        comp.BodyStatus = (args.NewValue > 1) ? BodyStatus.InAir : BodyStatus.OnGround;
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
        comp.MinRange = EventHorizonRadius(singulos) - 0.01f;
        (comp.BaseRadialAcceleration, comp.BaseTangentialAcceleration) = GravPulseAcceleration(singulos);
    }

#endregion Event Handlers
}
