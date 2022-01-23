using System;
using Content.Server.Dynamic.Abstract;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Dynamic.Conditions.Event;

public class CandidateCountCondition : GameEventCondition
{
    [DataField("min")]
    public int Min;

    // idk why you would use this here. but go for it if you want
    [DataField("max")]
    public int Max = Int32.MaxValue;

    public override bool Condition(GameEventData data, IEntityManager entityManager)
    {
        var count = data.Candidates.Count;
        return count > Min && count < Max;
    }
}
