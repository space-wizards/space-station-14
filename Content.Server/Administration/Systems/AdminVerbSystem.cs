using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.UI;
using Content.Server.Disposal.Tube;
using Content.Server.EUI;
using Content.Server.Ghost.Roles;
using Content.Server.Mind;
using Content.Server.Prayer;
using Content.Server.Silicons.Laws;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;
using Robust.Shared.Network;

namespace Content.Server.Administration.Systems;

/// <summary>
///     System to provide various global admin/debug verbs
/// </summary>
public sealed partial class AdminVerbSystem : SharedAdminVerbSystem
{
    [Dependency] private readonly IConGroupController _groupController = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AdminSystem _adminSystem = default!;
    [Dependency] private readonly DisposalTubeSystem _disposalTubes = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRoleSystem = default!;
    [Dependency] private readonly PrayerSystem _prayerSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLawSystem = default!;

    private readonly Dictionary<ICommonSession, List<EditSolutionsEui>> _openSolutionUis = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<SolutionContainerManagerComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    // This is a solution to deal with the fact theres no shared way to check command perms.
    // Should the ConGroupControllers be unified and shared, this should be replaced with that instead.
    public override bool CanCommandOverride(ICommonSession player, string command)
    {
        return _groupController.CanCommand(player, command);
    }

    public override void AdminPrayerVerb(ICommonSession player, ICommonSession target)
    {
        _quickDialog.OpenDialog(player,
            "Subtle Message",
            "Message",
            "Popup Message",
            (string message, string popupMessage) =>
            {
                _prayerSystem.SendSubtleMessage(target,
                    player,
                    message,
                    popupMessage == "" ? Loc.GetString("prayer-popup-subtle-default") : popupMessage);
            });
    }

    public override void AdminPlayerActionsSpawnVerb(EntityUid user, EntityUid target, ICommonSession targetSession)
    {
        if (!_transformSystem.TryGetMapOrGridCoordinates(target, out var coords))
        {
            _popup.PopupEntity(Loc.GetString("admin-player-spawn-failed"), user, user);
            return;
        }

        var stationUid = _stations.GetOwningStation(target);

        var profile = _gameTicker.GetPlayerProfile(targetSession);
        var mobUid = _spawning.SpawnPlayerMob(coords.Value, null, profile, stationUid);

        if (_mindSystem.TryGetMind(target, out var mindId, out var mindComp))
            _mindSystem.TransferTo(mindId, mobUid, true, mind: mindComp);
    }

    public override void AdminPlayerActionsCloneVerb(EntityUid user, EntityUid target, ICommonSession targetSession)
    {
        if (!_transformSystem.TryGetMapOrGridCoordinates(user, out var coords))
        {
            _popup.PopupEntity(Loc.GetString("admin-player-spawn-failed"), user, user);
            return;
        }

        var stationUid = _stations.GetOwningStation(target);

        var profile = _gameTicker.GetPlayerProfile(targetSession);
        _spawning.SpawnPlayerMob(coords.Value, null, profile, stationUid);
    }

    public override void AdminEraseVerb(NetUserId target)
    {
        _adminSystem.Erase(target);
    }

    public override void AdminEntityLogsVerb(ICommonSession player, EntityUid target)
    {
        var ui = new AdminLogsEui();
        _euiManager.OpenEui(ui, player);
        ui.SetLogFilter(search:target.Id.ToString());
    }

    public override void AdminSiliconLawsVerb(ICommonSession player, EntityUid target, SiliconLawBoundComponent comp)
    {
        var ui = new SiliconLawEui(_siliconLawSystem, EntityManager, _adminManager);
        _euiManager.OpenEui(ui, player);
        ui.UpdateLaws(comp, target);
    }

    public override void AdminCameraVerb(ICommonSession player, EntityUid target)
    {
        var ui = new AdminCameraEui(target);
        _euiManager.OpenEui(ui, player);
    }
}
