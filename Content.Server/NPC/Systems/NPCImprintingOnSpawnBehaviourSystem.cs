using System.Numerics;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Random;
using NPCImprintingOnSpawnBehaviourComponent = Content.Server.NPC.Components.NPCImprintingOnSpawnBehaviourComponent;

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

        if (entities.Count == 0)
            return;

        foreach (var friend in entities)
        {
            if (!(imprinting.Comp.Whitelist != null && !imprinting.Comp.Whitelist.IsValid(friend)))
            {
                AddImprintingTarget(imprinting, friend, imprinting.Comp);
            }
        }

        if (imprinting.Comp.Follow)
        {
            var mommy = _random.Pick(imprinting.Comp.Friends);
            _npc.SetBlackboard(imprinting, NPCBlackboard.FollowTarget, new EntityCoordinates(mommy, Vector2.Zero));
        }
    }

    public void AddImprintingTarget(EntityUid entity, EntityUid friend, NPCImprintingOnSpawnBehaviourComponent component)
    {
        component.Friends.Add(friend);
        var exception = EnsureComp<FactionExceptionComponent>(entity);
        exception.Ignored.Add(friend);
    }
}
