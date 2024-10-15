using Content.Server.GameTicking;
using Content.Server.Traits.Assorted;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._Impstation.Traits.Assorted;
public sealed class RandomUnrevivableSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomUnrevivableComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<RandomUnrevivableComponent> ent, ref ComponentStartup args)
    {
        //if we already have a revivable component, skip
        if (TryComp<UnrevivableComponent>(ent.Owner, out var revivableComp) == true)
        {
            return;
        }

        //else, roll the dice!
        var rand = _random.NextFloat(0f, 1f);
        if (rand <= ent.Comp.Chance)
        {
            var comp = EntityManager.AddComponent<UnrevivableComponent>(ent.Owner);
        }
    }
}
