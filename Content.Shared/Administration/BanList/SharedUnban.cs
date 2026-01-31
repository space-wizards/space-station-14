using Robust.Shared.Serialization;

namespace Content.Shared.Administration.BanList;

[Serializable, NetSerializable]
public sealed record SharedUnban(
    string? UnbanningAdmin,
    DateTime UnbanTime
);
