using Content.Shared.Toggleable;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class ToggleVisualsOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("enabled")]
    public bool Enabled = true;
    
    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var appearance = _entManager.System<SharedAppearanceSystem>();
        appearance.SetData(owner, ToggleableVisuals.Enabled, Enabled);
    }
}