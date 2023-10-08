using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Destructible;


namespace Content.Server.Spreader;

public sealed class ShadowKudzuSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    //I'm not sure about how good a decision it is to put variables here.
    //But if there will be hundreds of kudza instances on the map, to save memory,
    //let it be stored here. Besides, the logic is not processed for each instance anyway,
    //but for the whole system at once with random sampling.

    //About the system: Every time X instances of fog appear, a special loot spavener appears in a random spot in the fog.
    //This avoids endless monster spawning: If the fog has nowhere to grow, no new entities appear. 

    private EntProtoId _lootPrototype = "ShadowKudzuLootSpawner"; //the name of the entity that will periodically spawn in a random point of kudzu fog

    private const int KudzuForSpawner = 15; // how many kudzu entities should exist to generate 1 spawner
    private int _nextSpawn = 15;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowKudzuComponent, ComponentInit>(OnComponentInit);
    }
    private void OnComponentInit(EntityUid uid, ShadowKudzuComponent component, ComponentInit args)
    {
        _nextSpawn--;
        if (_nextSpawn <= 0)
        {
            _nextSpawn = KudzuForSpawner;
            SpawnLoot();
        }
    }

    private void SpawnLoot()
    {
        var query = EntityQuery<ShadowKudzuComponent>().ToList();
        var randomKudzu = _random.Pick(query);

        if (!TryComp<TransformComponent>(randomKudzu.Owner, out var transform))
            return;

        Spawn(_lootPrototype, transform.Coordinates);
    }
}
