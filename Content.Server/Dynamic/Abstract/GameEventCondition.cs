using Content.Server.Dynamic.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Dynamic.Abstract;

/// <summary>
///     Used by <see cref="GameEventPrototype"/> to determine whether it should run at all.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class GameEventCondition
{
    public abstract bool Condition(GameEventData data, IEntityManager entityManager);
}

