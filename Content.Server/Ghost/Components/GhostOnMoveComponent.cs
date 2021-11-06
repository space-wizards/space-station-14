using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IGhostOnMove))]
    public class GhostOnMoveComponent : Component,IGhostOnMove
    {
        public override string Name => "GhostOnMove";

        [DataField("canReturn")] public bool CanReturn { get; set; } = true;
    }
}
