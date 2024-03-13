using Robust.Shared.Random;

namespace Content.Server.ReplaceRandom;

public sealed class ReplaceRandomSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ReplaceRandomComponent, MapInitEvent>(OnMapInit);
    }

    public void OnMapInit(Entity<ReplaceRandomComponent> entity, ref MapInitEvent args)
    {
        if (!_random.Prob(entity.Comp.Chance)) return;
        SpawnNextToOrDrop(entity.Comp.Prototype, entity);
        QueueDel(entity);
    }
}
