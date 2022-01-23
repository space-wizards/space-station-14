using System;
using Content.Server.Dynamic.Abstract;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Dynamic.Conditions.Event;

public class PlayerCountCondition : GameEventCondition
{
    [DataField("min")]
    public int Min = 0;

    [DataField("max")]
    public int Max = Int32.MaxValue;

    public override bool Condition(GameEventData data, IEntityManager entityManager)
    {
        return data.PlayerCount > Min && data.PlayerCount < Max;
    }
}
