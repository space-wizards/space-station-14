using Content.Server.Administration.Logs;
using Content.Shared.Construction;
using Content.Shared.Database;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions;

/// <summary>
///     Generate an admin log upon reaching this node. Useful for dangerous construction (e.g., modular grenades)
/// </summary>
[UsedImplicitly]
public sealed partial class AdminLog : IGraphAction
{
    [DataField("logType")]
    public LogType LogType = LogType.Construction;

    [DataField("impact")]
    public LogImpact Impact = LogImpact.Medium;

    [DataField("message", required: true)]
    public string Message = string.Empty;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        var logManager = IoCManager.Resolve<IAdminLogManager>();

        if (userUid.HasValue)
            logManager.Add(LogType, Impact, $"{Message} - Entity: {entityManager.ToPrettyString(uid):entity}, User: {entityManager.ToPrettyString(userUid.Value):user}");
        else
            logManager.Add(LogType, Impact, $"{Message} - Entity: {entityManager.ToPrettyString(uid):entity}");
    }
}
