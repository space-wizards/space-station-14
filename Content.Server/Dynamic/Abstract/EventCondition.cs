using Content.Server.Dynamic.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Server.Dynamic.Abstract;

/// <summary>
///     Used by <see cref="GameEventPrototype"/> to determine whether it should run at all.
/// </summary>
public abstract class EventCondition
{
    public abstract bool Condition(GameEventData data, IEntityManager entityManager);
}

