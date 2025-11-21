using Content.Shared.Toggleable;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Sets enabled or disabled on a ToggleableVisuals enum, and communicates that change to the appearance system
/// If the entity using this has a visualizer that uses the ToggleableVisuals enum, this allows changing the sprite state
/// </summary>
/// <see cref="ToggleableVisuals"/>
public sealed partial class ToggleVisualsOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField]
    public bool Enabled = true;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var appearance = _entManager.System<SharedAppearanceSystem>();
        appearance.SetData(owner, ToggleableVisuals.Enabled, Enabled);
    }
}
