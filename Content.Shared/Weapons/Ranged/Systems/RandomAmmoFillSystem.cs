using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Ranged.Systems;

// Randomizes loaded ammo count for magazines with RandomAmmoFillComponent.
// Runs after SharedGunSystem so it overrides the UnspawnedCount = Capacity
// reset performed in OnBallisticMapInit.
public sealed class RandomAmmoFillSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomAmmoFillComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(SharedGunSystem) });
    }

    private void OnMapInit(Entity<RandomAmmoFillComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<BallisticAmmoProviderComponent>(ent.Owner, out var ballistic))
            return;

        var min = Math.Clamp((int) MathF.Round(ballistic.Capacity * ent.Comp.MinFillFraction), 0, ballistic.Capacity);
        var max = Math.Clamp((int) MathF.Round(ballistic.Capacity * ent.Comp.MaxFillFraction), 0, ballistic.Capacity);
        if (min > max)
            min = max;

        ballistic.UnspawnedCount = _random.Next(min, max + 1);
        Dirty(ent.Owner, ballistic);
    }
}
