using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

/// <summary>
///     Shared base implementation for <see cref="ISharedAdminLogManager"/>.
///     lives on the interface so it appears consistently in shared call sites and generated API docs.
/// </summary>
[Virtual]
public class SharedAdminLogManager : ISharedAdminLogManager
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public IEntityManager EntityManager => _entityManager;

    public bool Enabled { get; protected set; }

    /// <inheritdoc />
    public virtual string ConvertName(string name) => name;

    /// <inheritdoc />
    public virtual void Add(
        LogType type,
        LogImpact impact,
        ref LogStringHandler handler,
        object? payload = null,
        IReadOnlyCollection<Guid>? players = null,
        IReadOnlyCollection<AdminLogEntityRef>? entities = null,
        IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null)
    {
    }

    /// <inheritdoc />
    public virtual void Add(
        LogType type,
        ref LogStringHandler handler,
        object? payload = null,
        IReadOnlyCollection<Guid>? players = null,
        IReadOnlyCollection<AdminLogEntityRef>? entities = null,
        IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null)
    {
        Add(type, LogImpact.Medium, ref handler, payload, players, entities, playerRoles);
    }
}
