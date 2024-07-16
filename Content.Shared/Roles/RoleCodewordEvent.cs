using Robust.Shared.Serialization;

namespace Content.Shared.Roles;

/// <summary>
/// Raised on the server and sent to a client to record traitor codewords without exposing them to other clients.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoleCodewordEvent : EntityEventArgs
{
    public NetEntity Entity;
    public List<string> Codewords;

    public RoleCodewordEvent(NetEntity entity, List<string> codewords)
    {
        Codewords = codewords;
        Entity = entity;
    }
}
