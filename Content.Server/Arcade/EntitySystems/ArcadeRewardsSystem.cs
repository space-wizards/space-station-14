using System.Linq;
using Content.Server.Arcade.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Arcade.EntitySystems;

public sealed partial class ArcadeRewardsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Random _random = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcadeRewardsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ArcadeRewardsComponent> ent, ref MapInitEvent args)
    {
        var (_, component) = ent;

        // Random amount of rewards
        component.Amount = _robustRandom.Next(component.MinAmount, component.MaxAmount);
    }

    /// <summary>
    ///
    /// </summary>
    public void GiveReward(EntityUid uid, ArcadeRewardsComponent? component = null, TransformComponent? xForm = null)
    {
        if (!Resolve(uid, ref component, ref xForm) || component.Amount <= 0 || component.Rewards == null)
            return;

        var selectedEntity = component.Rewards.GetSpawns(_random, EntityManager, _prototypeManager).First().Id;
        EntityManager.SpawnEntity(selectedEntity, xForm.Coordinates);

        component.Amount--;
    }
}
