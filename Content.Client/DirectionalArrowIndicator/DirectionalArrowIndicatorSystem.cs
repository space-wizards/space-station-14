using Content.Shared.DirectionalArrowIndicator;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Client.DirectionalArrowIndicator;

public sealed class DirectionalArrowIndicatorSystem : EntitySystem
{
    private static readonly EntProtoId ExamineArrow = "DirectionalArrowIndicator";

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
        {
            SpawnArrow(new EntityCoordinates(ent, 0, 0), lifetime, Angle.Zero);
            return;
        }
        foreach (var arrowData in ent.Comp.Arrows)
        {
            SpawnArrow(new EntityCoordinates(ent, arrowData.Offset), lifetime, arrowData.Rotation);
        }
    }

    private void SpawnArrow(EntityCoordinates coords, float lifetime, Angle rotation)
    {
        var spawnedEnt = Spawn(ExamineArrow, coords);

        Transform(spawnedEnt).LocalRotation = rotation;

        if (TryComp<TimedDespawnComponent>(spawnedEnt, out var timedDespawn))
        {
            timedDespawn.Lifetime = lifetime;
        }
    }
}
