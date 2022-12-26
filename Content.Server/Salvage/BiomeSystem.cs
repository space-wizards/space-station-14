using Content.Shared.Salvage;
using Robust.Shared.Random;

namespace Content.Server.Salvage;

public sealed class BiomeSystem : SharedBiomeSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BiomeComponent, MapInitEvent>(OnBiomeMapInit);
    }

    private void OnBiomeMapInit(EntityUid uid, BiomeComponent component, MapInitEvent args)
    {
        component.Seed = _random.Next();
    }
}
