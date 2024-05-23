using Content.Shared.IconSmoothing;
using Robust.Shared.Random;

namespace Content.Server.IconSmoothing;

public sealed partial class RandomIconSmoothSystem : SharedRandomIconSmoothSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomIconSmoothComponent, ComponentStartup>(OnCompStartup);
    }

    private void OnCompStartup(Entity<RandomIconSmoothComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.RandomStates.Count == 0)
            return;

        var state = _random.Pick(ent.Comp.RandomStates);
        ent.Comp.SelectedState = state;
        Dirty(ent);
    }
}
