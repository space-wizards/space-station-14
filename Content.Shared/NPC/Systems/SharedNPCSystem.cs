namespace Content.Shared.NPC.Systems;

public abstract partial class SharedNPCSystem : EntitySystem
{
    public abstract bool IsNpc(EntityUid uid);
}
