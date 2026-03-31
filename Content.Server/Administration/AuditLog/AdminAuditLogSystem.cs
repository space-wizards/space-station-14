namespace Content.Server.Administration.AuditLog;

public sealed class AdminAuditLogSystem : EntitySystem
{
    [Dependency] private readonly IAdminAuditLogManager _auditLog = default!;

    public override void Initialize()
    {
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
