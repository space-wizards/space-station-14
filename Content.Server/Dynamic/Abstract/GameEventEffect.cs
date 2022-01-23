using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Dynamic.Abstract;

/// <summary>
///     An effect that is to be run when a game event is purchased.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class GameEventEffect
{
    public abstract void Effect(GameEventData data, IEntityManager entityManager);
}
