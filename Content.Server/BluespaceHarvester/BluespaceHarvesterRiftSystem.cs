using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.BluespaceHarvester;

public sealed class BluespaceHarvesterRiftSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private float _updateTimer = 0.0f;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer < UpdateTime)
            return;

        _updateTimer -= UpdateTime;

        var query = EntityQueryEnumerator<BluespaceHarvesterRiftComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            UpdatePassiveSpawn(uid, comp, xform);
            UpdateSpawn(uid, comp, xform);
        }
    }

    private void UpdatePassiveSpawn(EntityUid uid, BluespaceHarvesterRiftComponent comp, TransformComponent xform)
    {
        comp.PassiveSpawnAccumulator++;
        if (comp.PassiveSpawnAccumulator < comp.PassiveSpawnCooldown)
            return;

        comp.PassiveSpawnAccumulator -= comp.PassiveSpawnCooldown;

        // Random, not particularly dangerous mob.
        Spawn(_random.Pick(comp.PassiveSpawn), xform.Coordinates);
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

            var pick = _random.Pick(pickable);
            comp.Danger -= pick.Cost; // Deduct the risk spent on the purchase.
            Spawn(pick.Id, xform.Coordinates);
        }
    }
}
