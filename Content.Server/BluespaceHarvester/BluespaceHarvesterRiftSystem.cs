using Robust.Shared.Random;

namespace Content.Server.BluespaceHarvester;

public sealed class BluespaceHarvesterRiftSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BluespaceHarvesterRiftComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            comp.PassiveSpawnAccumulator += frameTime;
            if (comp.PassiveSpawnAccumulator >= comp.PassiveSpawnCooldown)
            {
                comp.PassiveSpawnAccumulator -= comp.PassiveSpawnCooldown;
                comp.PassiveSpawnAccumulator += _random.NextFloat(comp.PassiveSpawnCooldown / 2f);

                // Random, not particularly dangerous mob.
                Spawn(_random.Pick(comp.PassiveSpawn), xform.Coordinates);
            }

            comp.SpawnAccumulator += frameTime;
            if (comp.SpawnAccumulator >= comp.SpawnCooldown)
            {
                comp.SpawnAccumulator -= comp.SpawnCooldown;
                comp.PassiveSpawnAccumulator += _random.NextFloat(comp.SpawnCooldown);

                UpdateSpawn(uid, comp, xform);
            }
        }
    }

    private void UpdateSpawn(EntityUid uid, BluespaceHarvesterRiftComponent comp, TransformComponent xform)
    {
        var count = 0;
        while (comp.Danger != 0 && count < 3)
        {
            count++;

            var pickable = new List<EntitySpawn>();
            foreach (var spawn in comp.Spawn)
            {
                if (spawn.Cost <= comp.Danger)
                    pickable.Add(spawn);
            }

            // If we cannot choose anything, this means that we have used up all the danger sufficient before spawn.
            if (pickable.Count == 0)
            {
                comp.Danger = 0; // This will disable pointless spawn attempts.
                break;
            }

            // In order for there to be a dangerous mob,
            // it should still be a good story,
            // because they still have a whole cart of ordinary ones.
            var pick = _random.Pick(pickable);
            comp.Danger -= pick.Cost; // Deduct the risk spent on the purchase.
            Spawn(pick.Id, xform.Coordinates);
        }
    }
}
