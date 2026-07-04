using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
/// Shared datatypes and constants for the administrative quick info system.
/// </summary>
/// <remarks>
/// "Quick Info" Allows admins to quickly get basic information & action links
/// about (player) entities in nice little popups.
/// </remarks>
public static class QuickInfoShared
{
    public const AdminFlags RequiredFlag = AdminFlags.Adminhelp;
    public const string CommandName = "_admin_quick_info";

    [Serializable, NetSerializable]
    public sealed class Request : EntityEventArgs
    {
        public required NetEntity[] Entities;
    }

    [Serializable, NetSerializable]
    public sealed class Response : EntityEventArgs
    {
        public required SingleEntityInfo[] Entities;
    }

    [Serializable, NetSerializable]
    public sealed record SingleEntityInfo(NetEntity Entity, bool Exists, string Name, string? Prototype);
}
