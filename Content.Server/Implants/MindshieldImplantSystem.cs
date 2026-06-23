using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Implants;

namespace Content.Server.Implants;

public abstract partial class MindshieldImplantSystem : SharedMindshieldImplantSystem
{
    [Dependency] private IAdminLogManager _adminLogManager = default!;

    protected override void TryLog(EntityUid uid)
    {
        // we actually log the message on the server side
        _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to being implanted with a Mindshield.");
    }
}
