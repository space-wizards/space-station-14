using System.Runtime.CompilerServices;
using Content.Shared.Database;

namespace Content.Shared.Administration.Logs;

public interface ISharedAdminLogManager
{
    public bool Enabled { get; }

    // JsonNamingPolicy is not whitelisted by the sandbox.
    public string ConvertName(string name);

    // Required for the log string interpolation handler to access ToPrettyString()
    public IEntityManager EntityManager { get; }

    void Add(LogType type, LogImpact impact, [InterpolatedStringHandlerArgument("")] ref LogStringHandler handler);

    void Add(LogType type, [InterpolatedStringHandlerArgument("")] ref LogStringHandler handler);
}
