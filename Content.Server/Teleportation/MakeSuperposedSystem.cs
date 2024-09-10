using Robust.Shared.Random;

namespace Content.Server.Teleportation;

public sealed class MakeSuperposedSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MakeSuperposedComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<MakeSuperposedComponent> entity, ref MapInitEvent args)
    {
        RemCompDeferred<MakeSuperposedComponent>(entity);
        if (_random.NextFloat() > entity.Comp.Chance)
            return;
        EnsureComp<SuperposedComponent>(entity.Owner);
    }
}
