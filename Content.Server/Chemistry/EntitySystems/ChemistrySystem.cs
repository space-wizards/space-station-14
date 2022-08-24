using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        // Why ChemMaster duplicates reagentdispenser nobody knows.
        InitializeChemMaster();
        InitializeHypospray();
        InitializeInjector();
        InitializeReagentDispenser();
    }
}
