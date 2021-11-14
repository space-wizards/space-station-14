using Content.Server.Mind.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Mind.Systems
{
    public class VisitingMindSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VisitingMindComponent, ComponentRemove>(OnRemove);
        }

        private void OnRemove(EntityUid uid, VisitingMindComponent component, ComponentRemove args)
        {
            component.Mind?.UnVisit();
        }
    }
}
