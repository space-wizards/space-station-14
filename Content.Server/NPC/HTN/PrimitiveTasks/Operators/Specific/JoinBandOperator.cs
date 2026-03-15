using Content.Server.Instruments;
using Content.Shared.Instruments;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

/// <summary>
/// The owner accompanies the music played by the target entity
/// This is the NPC equivalent to using the 'join band' menu
/// If there is no target, or the target is not playing music, will instead stop playing
/// </summary>
public sealed partial class JoinBandOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField]
    public string TargetKey = "Target";

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entManager.TryGetComponent<InstrumentComponent>(owner, out var instrument))
            return;
        
        var instrumentSystem = _entManager.System<InstrumentSystem>();

        // If target is null, clean and deactivate instrument
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
        {
            instrumentSystem.DeactivateInstrument(owner);
            instrumentSystem.Clean(owner, instrument);
            return;
        }

        instrumentSystem.PrepareInstrument(owner);
        instrumentSystem.SetMaster(owner, target);
    }
}
