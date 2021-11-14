using Content.Server.Mind.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Mind.Systems
{
    public class VisitingMindSystem : EntitySystem
    {
        [Dependency] private readonly MindSystem _mindSys = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VisitingMindComponent, ComponentRemove>(OnRemove);
        }

        private void OnRemove(EntityUid uid, VisitingMindComponent component, ComponentRemove args)
        {
            if (component.Mind == null)
                return;

            _mindSys.UnVisit(component.Mind);
        }
    }
}
