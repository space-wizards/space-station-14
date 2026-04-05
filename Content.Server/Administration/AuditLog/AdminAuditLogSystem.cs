using Content.Server.GameTicking.Events;

namespace Content.Server.Administration.AuditLog;

public sealed class AdminAuditLogSystem : EntitySystem
{
    [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(ev => _auditLog.RoundStarting(ev.Id));
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
