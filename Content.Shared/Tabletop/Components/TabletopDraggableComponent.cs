using Robust.Shared.GameObjects;

namespace Content.Shared.Tabletop.Components
{
    [RegisterComponent]
    public class TabletopDraggableComponent : Component
    {
        public override string Name => "TabletopDraggable";

        public bool CanStartDrag()
        {
            // TODO: implement permissions; only allow specific users to move object (may or may not be necessery)

            return true;
        }
    }
}
