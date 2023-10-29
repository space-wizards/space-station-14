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
        Spawn(_random.Pick(comp.PassiveSpawnPrototypes), xform.Coordinates);
    }

    private void UpdateSpawn(EntityUid uid, BluespaceHarvesterRiftComponent comp, TransformComponent xform)
    {
        var count = 0;

        while (comp.Danger != 0 && count < 3)
        {
            count++;

            var pickable = comp.SpawnPrototypes.Where((spawn) => spawn.Cost <= comp.Danger).ToList();

            if (pickable.Count == 0)
            {
                comp.Danger = 0;
                break;
            }

            var pick = _random.Pick(pickable);
            comp.Danger -= pick.Cost;
            Spawn(pick.PrototypeId, xform.Coordinates);
        }
    }
}
