using Content.Server.Antag.Mimic;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Antag;

/// <summary>
/// Game rule handler: replaces random vending machine entities with arbitrary prototypes.
/// </summary>
/// <seealso cref="MobReplacementRuleComponent"/>
/// <seealso cref="VendingMachineComponent"/>
public sealed partial class MobReplacementRuleSystem : GameRuleSystem<MobReplacementRuleComponent>
{
    [Dependency] private IRobustRandom _random = default!;

    protected override void Started(Entity<MobReplacementRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var query = AllEntityQuery<VendingMachineComponent, TransformComponent>();
        var spawns = new List<(EntityUid Entity, EntityCoordinates Coordinates)>();

        while (query.MoveNext(out var vendingUid, out _, out var xform))
        {
            if (!_random.Prob(ent.Comp1.Chance))
                continue;

            spawns.Add((vendingUid, xform.Coordinates));
        }

        foreach (var entity in spawns)
        {
            var coordinates = entity.Coordinates;
            Del(entity.Entity);

            Spawn(ent.Comp1.Proto, coordinates);
        }
    }
}
