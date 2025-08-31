using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.NPC.Systems;

public sealed class NPCPointDefenseSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCPointDefenseComponent, GunShotEvent>(GunShotEvent);
    }

    private void GunShotEvent(EntityUid uid, NPCPointDefenseComponent component, ref GunShotEvent args)
    {
        if (!TryComp<HTNComponent>(uid, out var htn) || !htn.Blackboard.TryGetValue<EntityUid>(component.TargetKey, out var target, EntityManager))
            return;

        QueueDel(target);
    }
}
