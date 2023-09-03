using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Shared.CombatMode;
using Content.Shared.Chemistry;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private InteractionSystem _interaction = default!;
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ReactiveSystem _reactiveSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        // Why ChemMaster duplicates reagentdispenser nobody knows.
        InitializeHypospray();
        InitializeInjector();
        InitializeMixing();
    }
}
