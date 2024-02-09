using Content.Server.PowerCell;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class PowerCellExistsPrecondition : HTNPrecondition
{
    private PowerCellSystem _powercell = default!;
    [DataField] public bool Exists = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _powercell = sysManager.GetEntitySystem<PowerCellSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        _powercell.TryGetBatteryFromSlot(owner, out EntityUid? cell, out _);
        if (cell is {})
            return Exists;

        return !Exists;
    }
}
