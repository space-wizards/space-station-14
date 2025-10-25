using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

[Virtual]
public class SharedAdminLogManager : ISharedAdminLogManager
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
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
}
