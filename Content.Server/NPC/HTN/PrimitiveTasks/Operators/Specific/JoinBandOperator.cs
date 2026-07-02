using System.ComponentModel;
using Content.Server.Instruments;
using Content.Shared.Instruments;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

/// <summary>
/// The owner accompanies the music played by the target entity
/// This is the NPC equivalent to using the 'join band' menu
/// If ExitBand = true, will instead leave bands
/// </summary>
public sealed partial class JoinBandOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private InstrumentSystem _instrument = default!;

    [DataField]
    public string TargetKey = "Target";

    [DataField]
    public bool ExitBand = false;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _instrument = sysManager.GetEntitySystem<InstrumentSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        if (ExitBand)
            blackboard.Remove<EntityUid>(TargetKey);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entManager.TryGetComponent<InstrumentComponent>(owner, out var instrument))
            return;

        // If target is null, clean and deactivate instrument
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
        {
            _instrument.DeactivateInstrument(owner);
            _instrument.Clean(owner, instrument);
            return;
        }

        _instrument.PrepareInstrument(owner);
        _instrument.SetMaster(owner, instrument, target);
    }
}
