using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using System.Numerics;

namespace Content.Client.DirectionalArrowIndicator;

public sealed class DirectionalArrowIndicatorSystem : EntitySystem
{
    private const float Edge_offset = 0.78125f;

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
            var spawnedEnt = Spawn(arrowData.ArrowType, new EntityCoordinates(ent, arrowData.Offset.X, arrowData.Offset.Y + Edge_offset));

            Transform(spawnedEnt).LocalRotation = arrowData.Rotation;

            if (EnsureComp<TimedDespawnComponent>(spawnedEnt, out var timedDespawn))
            {
                timedDespawn.Lifetime = lifetime;
            }
        }
    }
}
