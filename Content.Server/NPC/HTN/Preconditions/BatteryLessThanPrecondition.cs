using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class PowerCellComparisonPrecondition : HTNPrecondition
{
    private PowerCellSystem _powercell = default!;
    [DataField("useWatts")] public bool UseWatts;
    [DataField("greaterThan")] public bool GreaterThan;
    [DataField("percent")] public float? Percentage;
    [DataField("watts")] public float? Watts;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _powercell = sysManager.GetEntitySystem<PowerCellSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);


        return GreaterThan; // not implemented lol
    }
}
