using Content.Server.VentHorde.Components;
using Content.Shared.Destructible;
using Content.Shared.Jittering;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.VentHorde.Systems;

public sealed class VentHordeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentHordeSpawnerComponent, MapInitEvent>(OnSpawnerInit);
        SubscribeLocalEvent<VentHordeSpawnerComponent, ComponentShutdown>(OnSpawnerShutdown);

        SubscribeLocalEvent<VentHordeSpawnerComponent, BreakageEventArgs>(OnSpawnerBreakage);
        SubscribeLocalEvent<VentHordeSpawnerComponent, AnchorStateChangedEvent>(OnSpawnerAnchored);
    }

    private void OnSpawnerInit(Entity<VentHordeSpawnerComponent> entity, ref MapInitEvent args)
    {
        _jitter.AddJitter(entity);
    }

    private void OnSpawnerShutdown(Entity<VentHordeSpawnerComponent> entity, ref ComponentShutdown args)
    {
        _audio.Stop(entity.Comp.AudioStream);
        RemCompDeferred<JitteringComponent>(entity);
    }

    private void OnSpawnerBreakage(Entity<VentHordeSpawnerComponent> entity, ref BreakageEventArgs args)
    {
        // There is no escape.
        EndHordeSpawn(entity);
    }

    private void OnSpawnerAnchored(Entity<VentHordeSpawnerComponent> entity, ref AnchorStateChangedEvent args)
    {
        // Anchor state changes when the entity is broken, to avoid double spawning we check if the entity is gonna be deleted.
        if (TerminatingOrDeleted(entity))
            return;

        // There is no escape.
        EndHordeSpawn(entity);
    }

    /// <summary>
    /// Starts a horde spawn at an entity.
    /// </summary>
    /// <param name="uid">The entity to spawn the horde at.</param>
    /// <param name="spawns">List of entities to spawn.</param>
    /// <param name="spawnDelay">Time after which to spawn the entities.</param>
    /// <param name="append">If an already active spawner is selected, will add entities to its list. Otherwise, will fail.</param>
    public void StartHordeSpawn(EntityUid uid, List<EntProtoId> spawns, TimeSpan spawnDelay, bool append = true)
    {
        if (TryComp<VentHordeSpawnerComponent>(uid, out var hordeSpawner))
        {
            if (append)
            {
                hordeSpawner.Entities.AddRange(spawns);
            }

            return;
        }

        hordeSpawner = EnsureComp<VentHordeSpawnerComponent>(uid);

        hordeSpawner.AudioStream = _audio.PlayPvs(hordeSpawner.PassiveSound, uid, hordeSpawner.PassiveSound.Params.WithLoop(true))?.Entity;

        hordeSpawner.Entities = spawns;
        hordeSpawner.SpawnTime = _timing.CurTime + spawnDelay;
    }

    /// <summary>
    /// Ends a horde spawn, causing all entities to spawn at once.
    /// </summary>
    /// <param name="entity">The horde spawner entity.</param>
    public void EndHordeSpawn(Entity<VentHordeSpawnerComponent> entity)
    {
        entity.Comp.AudioStream = _audio.Stop(entity.Comp.AudioStream);

        _audio.PlayPvs(entity.Comp.EndSound, entity);

        foreach (var spawn in entity.Comp.Entities)
        {
            var spawned = Spawn(spawn, Transform(entity).Coordinates);
            var direction = _random.NextVector2() * _random.NextFloat(entity.Comp.MinThrowDistance, entity.Comp.MaxThrowDistance);
            var throwSpeed = _random.NextFloat(entity.Comp.MinThrowSpeed, entity.Comp.MaxThrowSpeed);
            _throwing.TryThrow(spawned, direction, throwSpeed);
        }

        RemCompDeferred<VentHordeSpawnerComponent>(entity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VentHordeSpawnerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.SpawnTime != null && _timing.CurTime > comp.SpawnTime)
            {
                EndHordeSpawn((uid, comp));
            }
        }
    }
}
