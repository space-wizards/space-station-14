using System.Text.Json;
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
        string message,
        JsonDocument json,
        IReadOnlyCollection<Guid>? players = null,
        IReadOnlyCollection<AdminLogEntityRef>? entities = null,
        IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null)
    {
        var handler = new LogStringHandler(message.Length, 0, this, out var isEnabled);
        if (!isEnabled)
            return;

        handler.AppendLiteral(message);
        Add(type, impact, ref handler);
    }
}
