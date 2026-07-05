using Content.Shared.Examine;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Spawners;

namespace Content.Client.DirectionalArrowIndicator;

/// <summary>
/// System responsible for handling <see cref="DirectionalArrowIndicatorComponent"/>s,
/// spawning directional arrow indicators clientside when an entity is examined.
/// </summary>
public sealed partial class DirectionalArrowIndicatorSystem : EntitySystem
{
    [Dependency] private TransformSystem _transform = default!;

    private const float EdgeOffset = 0.78125f; // Used for moving arrow to the edge of the tile by default.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DirectionalArrowIndicatorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<DirectionalArrowIndicatorComponent> ent, ref ExaminedEvent args)
    {
        var arrows = ent.Comp.Arrows;
        var lifetime = ent.Comp.Lifetime;

        if (arrows.Count == 0)
            arrows.Add(new ArrowSpawnData());

        foreach (var arrowData in ent.Comp.Arrows)
        {
            var spawnedEnt = Spawn(arrowData.ArrowType, new EntityCoordinates(ent, arrowData.Offset.X, arrowData.Offset.Y + EdgeOffset));

            _transform.SetLocalRotation(spawnedEnt, arrowData.Rotation);

            EnsureComp<TimedDespawnComponent>(spawnedEnt, out var timedDespawn);
            timedDespawn.Lifetime = lifetime;
        }
    }
}
