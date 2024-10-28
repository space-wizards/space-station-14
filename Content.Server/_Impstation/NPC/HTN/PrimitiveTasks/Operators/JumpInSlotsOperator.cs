using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

public sealed partial class JumpInSlotsOperator : HTNOperator
{
    private InventorySystem _inventory = default!;

    [DataField("targetKey")]
    public string Key = "Target";

    /// <summary>
    /// If true, a failed attempt to jump into slots will still return <see cref="HTNOperatorStatus.Finished"/>
    /// </summary>
    [DataField]
    public bool IgnoreFail = false;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _inventory = sysManager.GetEntitySystem<InventorySystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(Key);

        var result = _inventory.TryJumpIntoSlots(owner, target);

        return result || IgnoreFail ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;
    }
}
