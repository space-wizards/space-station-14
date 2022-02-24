using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly AdminLogSystem _logs = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        InitializeHypospray();
        InitializeInjector();
    }
}
