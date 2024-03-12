using System.Collections.Frozen;
using System.Linq;
using System.Numerics;
using Content.Server.Mind.Toolshed;
using Content.Server.NPC.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCDuckingSyndromeSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NPCDuckingSyndromeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NPCDuckingSyndromeComponent> duck, ref MapInitEvent args)
    {
        var entities = _lookup.GetEntitiesInRange(duck, duck.Comp.SearchRadius);
        foreach (var ent in entities)
        {
            if (HasComp<ActorComponent>(ent))
            {
                duck.Comp.SyndromeTarget.Add(ent);
                var exception = EnsureComp<FactionExceptionComponent>(duck);
                exception.Ignored.Add(ent);
            }
            //if we haven't found mommy, we'll be aggressive with everyone.
        }
        var mommy = _random.Pick(duck.Comp.SyndromeTarget);

        _npc.SetBlackboard(duck, NPCBlackboard.FollowTarget, new EntityCoordinates(mommy, Vector2.Zero));
        return;
    }
}
