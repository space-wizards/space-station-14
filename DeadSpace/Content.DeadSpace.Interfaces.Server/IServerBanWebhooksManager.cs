using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;

namespace Content.DeadSpace.Interfaces.Server;

public interface IServerBanWebhooksManager
{
    Task SendBan(string? target, string? admin, uint? minutes, string reason, DateTimeOffset? expires, string? job, int color, string ban_type, int? roundId);
    Task SendDepartmentBan(string target, ICommonSession? admin, string department, string reason, uint minutes);
}
