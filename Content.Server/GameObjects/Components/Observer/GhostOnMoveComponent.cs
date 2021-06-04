#nullable enable
using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.Console;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Observer
{
    [RegisterComponent]
    [ComponentReference(typeof(IGhostOnMove))]
    public class GhostOnMoveComponent : Component, IRelayMoveInput, IGhostOnMove
    {
        public override string Name => "GhostOnMove";
        [Dependency] private readonly IGameTicker _gameTicker = default!;

        [DataField("canReturn")] public bool CanReturn { get; set; } = true;

        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            // Let's not ghost if our mind is visiting...
            if (Owner.HasComponent<VisitingMindComponent>()) return;
            if (!Owner.TryGetComponent(out MindComponent? mind) || !mind.HasMind || mind.Mind!.IsVisitingEntity) return;

            _gameTicker.OnGhostAttempt(mind.Mind!, CanReturn);
        }
    }
}
