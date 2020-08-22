using Content.Server.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class VisitingMindComponent : Component
    {
        public override string Name => "VisitingMind";

        public Mind Mind { get; set; }

        public override void OnRemove()
        {
            base.OnRemove();

            Mind?.UnVisit();
        }
    }
}
