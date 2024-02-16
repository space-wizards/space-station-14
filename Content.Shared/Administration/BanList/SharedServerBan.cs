using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.BanList;

[Serializable, NetSerializable]
public record SharedServerBan(
    int? Id,
    NetUserId? UserId,
    (string address, int cidrMask)? Address,
    string? HWId,
    DateTime BanTime,
    DateTime? ExpirationTime,
    string Reason,
    string? BanningAdminName,
    SharedServerUnban? Unban
);
