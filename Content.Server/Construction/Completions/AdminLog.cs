using Content.Server.Administration.Logs;
using Content.Shared.Construction;
using Content.Shared.Database;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions;

/// <summary>
///     Generate an admin log upon reaching this node. Useful for dangerous construction (e.g., modular grenades)
/// </summary>
[UsedImplicitly]
public sealed class AdminLog : IGraphAction
{
    [DataField("logType", required: true)]
    public LogType LogType = LogType.Construction;

    [DataField("impact")]
    public LogImpact Impact = LogImpact.Medium;

    [DataField("message", required: true)]
    public string Message = string.Empty;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        var adminLogs = IoCManager.Resolve<IAdminLogManager>();

        if (userUid.HasValue)
            adminLogs.Add(LogType, Impact, $"{Message} - Entity: {entityManager.ToPrettyString(uid):entity}, User: {entityManager.ToPrettyString(userUid.Value):user}");
        else
            adminLogs.Add(LogType, Impact, $"{Message} - Entity: {entityManager.ToPrettyString(uid):entity}");
    }
}
