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
        foreach (var ent in entities)
        {
            if (HasComp<ActorComponent>(ent))
            {
                impriting.Comp.ImpritingTarget.Add(ent);
                var exception = EnsureComp<FactionExceptionComponent>(impriting);
                exception.Ignored.Add(ent);
            }
            //if we haven't found mommy, we'll be aggressive with everyone.
        }

        if (impriting.Comp.Follow)
        {
            var mommy = _random.Pick(impriting.Comp.ImpritingTarget);
            _npc.SetBlackboard(impriting, NPCBlackboard.FollowTarget, new EntityCoordinates(mommy, Vector2.Zero));
        }

        return;
    }
}
