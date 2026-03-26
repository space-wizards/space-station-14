using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

[Virtual]
public partial class SharedAdminLogManager : ISharedAdminLogManager
{
    [Dependency] private IEntityManager _entityManager = default!;
    public IEntityManager EntityManager => _entityManager;

    public bool Enabled { get; protected set; }

    public virtual string ConvertName(string name) => name;

    public virtual void Add(LogType type, LogImpact impact, ref LogStringHandler handler)
    {
        // noop
    }

    public virtual void Add(LogType type, ref LogStringHandler handler)
    {
        // noop
    }

    public virtual void AddStructured(
        LogType type,
        LogImpact impact,
        ref LogStringHandler handler,
        object? payload = null,
        IReadOnlyCollection<Guid>? players = null,
        IReadOnlyCollection<AdminLogEntityRef>? entities = null,
        IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null)
    {
        // Client-side fallback: just forward to Add() for the message.
        Add(type, impact, ref handler);
    }
}
