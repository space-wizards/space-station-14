using Robust.Shared.Serialization;

namespace Content.Shared.Administration.BanList;

[Serializable, NetSerializable]
public sealed record SharedServerUnban(
    string? UnbanningAdmin,
    DateTime UnbanTime
);
