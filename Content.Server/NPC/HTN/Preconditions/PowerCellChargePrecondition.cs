using System.Diagnostics;
using Content.Server.PowerCell;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class PowerCellChargePrecondition : HTNPrecondition
{
    private PowerCellSystem _powerCell = default!;
    [DataField("greaterThan")] public bool GreaterThan = false;
    [DataField("percent")] public float? Percentage;
    [DataField("watts")] public float? Watts;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _powerCell = sysManager.GetEntitySystem<PowerCellSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (Watts is not null)
        {
            if (_powerCell.HasCharge(owner, Watts.Value))
                return GreaterThan;
            return !GreaterThan;
        }
        else if (Percentage is not null)
        {
            if (_powerCell.HasCharge(owner, Percentage.Value, percentage: true))
                return GreaterThan;
            return !GreaterThan;
        }

        throw new NotImplementedException();
    }
}
