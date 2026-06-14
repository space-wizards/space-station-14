using Robust.Shared.Timing;

namespace Content.Shared.Effects;

public sealed partial class ScreechShockWaveSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ScreechShockWaveComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ScreechShockWaveComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.InitTime = _timing.CurTime;
    }
}
