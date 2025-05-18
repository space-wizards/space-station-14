using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Salvage.JobBoard;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class JobBoardCommand : ToolshedCommand
{
    /// <summary> Completes a bounty automatically. </summary>
    [CommandImplementation("completeJob")]
    public void CompleteJob([PipedArgument] EntityUid station, ProtoId<CargoBountyPrototype> job)
    {
        if (!TryComp<SalvageJobsDataComponent>(station, out var salvageJobData))
            return;

        var sys = EntityManager.System<SalvageJobBoardSystem>();
        sys.TryCompleteSalvageJob((station, salvageJobData), job);
    }
}
