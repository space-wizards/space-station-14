using Content.Server.Arcade.Components;
using Content.Shared.EntityTable;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.Arcade.EntitySystems;

public sealed partial class ArcadeRewardsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityTableSystem _entityTableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcadeRewardsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ArcadeRewardsComponent> ent, ref MapInitEvent args)
    {
        var (_, component) = ent;

        // Random amount of rewards
        component.Amount = _random.Next(component.MinAmount, component.MaxAmount);
    }

    /// <summary>
    ///
    /// </summary>
    public void GiveReward(EntityUid uid, ArcadeRewardsComponent? component = null, TransformComponent? xForm = null)
    {
        if (!Resolve(uid, ref component, ref xForm))
            return;


        component.Amount--;
    }
}
