using System.Numerics;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCImprintingOnSpawnBehaviourSystem : SharedNPCImprintingOnSpawnBehaviourSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NPCImprintingOnSpawnBehaviourComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NPCImprintingOnSpawnBehaviourComponent> imprinting, ref MapInitEvent args)
    {

        var entities = _lookup.GetEntitiesInRange(imprinting, imprinting.Comp.SearchRadius);
        var imprintingTargets = new List<EntityUid>();

        if (entities == null) return;
        if (entities.Count == 0) return;

        foreach (var ent in entities)
        {
            var check = imprinting.Comp.Whitelist != null && !imprinting.Comp.Whitelist.IsValid(ent);
            if (!check)
            {
                imprintingTargets.Add(ent);
                var exception = EnsureComp<FactionExceptionComponent>(imprinting);
                exception.Ignored.Add(ent);
            }
        }

        if (imprinting.Comp.Follow)
        {
            var mommy = _random.Pick(imprintingTargets);
            _npc.SetBlackboard(imprinting, NPCBlackboard.FollowTarget, new EntityCoordinates(mommy, Vector2.Zero));
        }
    }

    private void AddImprintingTarget()
    {

    }
}
