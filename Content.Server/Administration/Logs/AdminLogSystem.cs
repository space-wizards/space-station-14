using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;

namespace Content.Server.Administration.Logs;

public sealed class AdminLogSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(ev => _adminLogs.RoundStarting(ev.Id));
        SubscribeLocalEvent<GameRunLevelChangedEvent>(ev => _adminLogs.RunLevelChanged(ev.New));
    }
}
