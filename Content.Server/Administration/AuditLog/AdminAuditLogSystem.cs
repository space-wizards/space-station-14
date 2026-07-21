using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;

namespace Content.Server.Administration.AuditLog;

public sealed class AdminAuditLogSystem : EntitySystem
{
    [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(ev => _auditLog.RoundStarting(ev.Id));
        SubscribeLocalEvent<GameRunLevelChangedEvent>(ev => _auditLog.RunLevelChanged(ev.New));
        _auditLog.Initialize();
    }

    public override void Shutdown()
    {
        _auditLog.Shutdown();
    }

    public override void Update(float frameTime)
    {
        _auditLog.Update();
    }
}
