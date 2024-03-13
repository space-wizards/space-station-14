using System.Numerics;
using Content.Server.NPC.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCImpritingBehaviourSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NPCImpritingBehaviourComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NPCImpritingBehaviourComponent> impriting, ref MapInitEvent args)
    {
        var entities = _lookup.GetEntitiesInRange(impriting, impriting.Comp.SearchRadius);
        var impritingTargets = new List<EntityUid>();
        foreach (var ent in entities)
        {
            if (impriting.Comp.Whitelist.IsValid(ent))
            {
                impritingTargets.Add(ent);
                var exception = EnsureComp<FactionExceptionComponent>(impriting);
                exception.Ignored.Add(ent);
            }
            //if we haven't found mommy, we'll be aggressive with everyone.
        }

        if (impriting.Comp.Follow)
        {
            var mommy = _random.Pick(impritingTargets);
            _npc.SetBlackboard(impriting, NPCBlackboard.FollowTarget, new EntityCoordinates(mommy, Vector2.Zero));
        }

        return;
    }
}
