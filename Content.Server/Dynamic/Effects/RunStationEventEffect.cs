using Content.Server.Dynamic.Abstract;
using Content.Server.StationEvents;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Dynamic.Effects;

public class RunStationEventEffect : GameEventEffect
{
    [DataField("name", required: true)]
    public string Name = default!;

    public override void Effect(GameEventData data, IEntityManager entityManager)
    {
        EntitySystem.Get<StationEventSystem>().RunEvent(Name);
    }
}
