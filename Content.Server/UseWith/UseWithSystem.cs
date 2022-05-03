using Content.Shared.Interaction;
using Content.Shared.Random.Helpers;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.UseWith;

public sealed class UseWithSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = null!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseWithComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, UseWithComponent component, InteractUsingEvent args)
    {
        if (component.UseWithWhitelist?.IsValid(args.Used) == false) return;
        
        for (var i = 0; i < component.SpawnCount; i++)
        {
            var getResult = EntitySpawnCollection.GetSpawns(component.Results, _random)[0];
            var playerPos = Transform(uid).MapPosition;
            var spawnPos = playerPos.Offset(_random.NextVector2(0.3f));
            var spawnResult = Spawn(getResult, spawnPos);
            spawnResult.RandomOffset(0.25f);
        }

        QueueDel(uid);
    }
}
