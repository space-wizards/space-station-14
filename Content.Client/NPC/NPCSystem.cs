using Content.Client.NPC.HTN;
using Content.Shared.NPC.Systems;

namespace Content.Client.NPC;

public sealed class NPCSystem : SharedNPCSystem
{
    public override bool IsNpc(EntityUid uid)
    {
        return HasComp<HTNComponent>(uid);
    }
}
