using Content.Shared.Toggleable;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Sets enabled or disabled on a ToggleableVisuals enum, and communicates that change to the appearance system
/// If the entity using this has a visualizer that uses the ToggleableVisuals enum, this allows changing the sprite state
/// </summary>
/// <see cref="ToggleableVisuals"/>
public sealed partial class ToggleVisualsOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private SharedAppearanceSystem _appearance = default!;

    [DataField]
    public bool Enabled = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _appearance = sysManager.GetEntitySystem<SharedAppearanceSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _appearance.SetData(owner, ToggleableVisuals.Enabled, Enabled);
    }
}
