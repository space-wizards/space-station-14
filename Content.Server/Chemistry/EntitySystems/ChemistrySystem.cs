using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Shared.CombatMode;
using Content.Shared.Chemistry;
using Content.Shared.MobState.EntitySystems;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        // Why ChemMaster duplicates reagentdispenser nobody knows.
        InitializeHypospray();
        InitializeInjector();
        InitializeMixing();
    }
}
