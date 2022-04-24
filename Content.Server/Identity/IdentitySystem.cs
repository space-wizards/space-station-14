using Content.Shared.Database;
using Content.Shared.Identity.Systems;

namespace Content.Server.Identity;

public sealed class IdentitySystem : SharedIdentitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ReportedEntityIdentityNetworkEvent>(OnReportIdentity);
    }

    private void OnReportIdentity(ReportedEntityIdentityNetworkEvent ev)
    {
        if (ev.Viewer == null)
        {
            _adminLogs.Add(LogType.Identity, LogImpact.Low,
                $"{ToPrettyString(ev.Target)} was reportedly viewed as {ev.ReportedIdentity}");
        }
        else
        {
            _adminLogs.Add(LogType.Identity, LogImpact.Low,
                $"{ToPrettyString(ev.Viewer.Value)} reported that it viewed {ToPrettyString(ev.Target)} as \"{ev.ReportedIdentity}\"");
        }
    }
}
