#nullable enable
using Content.Server.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class VisitingMindComponent : Component
    {
        public override string Name => "VisitingMind";

        [ViewVariables] public Mind Mind { get; set; } = default!;

        public override void OnRemove()
        {
            base.OnRemove();

            Mind?.UnVisit();
        }
    }

    public class MindUnvisitedMessage : EntityEventArgs
    {
    }
}
