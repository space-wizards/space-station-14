using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events;

/// <summary>
/// Event raised to indicate that the player wants to take direct control of a different entity.
/// </summary>
/// <remarks>This is only to be used for certain kinds of debug, as it breaks some expectations regarding minds
/// (it retains the current one, even when targeting a ghostrole's mob)</remarks>
/// <param name="target">The entity that the player is trying to control</param>
[NetSerializable, Serializable]
public sealed class DebugControlEntityEvent(NetEntity target) : EntityEventArgs
{
    public NetEntity Target = target;
}
