using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Interaction.Events;

/// <summary>
/// Raised on the client to the server requesting an interaction via the Use key.
/// </summary>
[Serializable, NetSerializable]
public sealed class InteractionRequestEvent(NetEntity user, NetEntity target, NetEntity source, Vector2 vector, bool utilityInteraction) : EntityEventArgs
{
    public NetEntity User = user;
    public NetEntity Target = target;

    /// <summary>
    /// This is the parent of the user. If they're on a grid, the grid UID is the source.
    /// </summary>
    public NetEntity Source = source;

    /// <summary>
    /// The vector is the position on said source. Both are used to reconstruct EntityCoordinates.
    /// </summary>
    public Vector2 Vector = vector;
    public bool UtilityInteraction = utilityInteraction;
}
