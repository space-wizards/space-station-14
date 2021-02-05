#nullable enable
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostOnMoveComponent : Component, IRelayMoveInput
    {
        public override string Name => "GhostOnMove";

        public bool CanReturn { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.CanReturn, "canReturn", true);
        }

        public void MoveInputPressed(ICommonSession session)
        {
            // Let's not ghost if our mind is visiting...
            if (Owner.HasComponent<VisitingMindComponent>()) return;
            if (!Owner.TryGetComponent(out MindComponent? mind) || !mind.HasMind || mind.Mind!.IsVisitingEntity) return;

            IoCManager.Resolve<IGameTicker>().OnGhostAttempt(mind.Mind, CanReturn);
        }
    }
}
