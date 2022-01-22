using Robust.Shared.GameObjects;

namespace Content.Server.Dynamic.Abstract;

/// <summary>
///     An effect that is to be run when a game event is purchased.
/// </summary>
public abstract class GameEventEffect
{
    public abstract void Effect(GameEventData data, IEntityManager entityManager);
}
