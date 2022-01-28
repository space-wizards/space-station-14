using Content.Server.Dynamic.Abstract;
using Robust.Shared.GameObjects;

namespace Content.Server.Dynamic.Conditions.Event;

/// <summary>
///     For events that should always be refunded.
/// </summary>
public class AlwaysCondition : GameEventCondition
{
    public override bool Condition(GameEventData data, IEntityManager entityManager)
    {
        return true;
    }
}
