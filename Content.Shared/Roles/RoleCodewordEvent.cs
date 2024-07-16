using Robust.Shared.Serialization;

namespace Content.Shared.Roles;

/// <summary>
/// Raised on the server and sent to a client to record traitor codewords without exposing them to other clients.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoleCodewordEvent : EntityEventArgs
{
    public NetEntity Entity;
    public Color Color;
    public string RoleKey;
    public List<string> Codewords;

    /// <summary>
    /// Raised on the server and sent to a client to record traitor codewords without exposing them to other clients.
    /// </summary>
    /// <param name="entity">The entity which the codewords should be assigned to.</param>
    /// <param name="color">The color the keywords should be highlighted in.</param>
    /// <param name="roleKey">The key used to distinguish the codewords for a specific role.</param>
    /// <param name="codewords">The list of codewords.</param>
    public RoleCodewordEvent(NetEntity entity, Color color, string roleKey, List<string> codewords)
    {
        Codewords = codewords;
        Color = color;
        RoleKey = roleKey;
        Entity = entity;
    }
}
