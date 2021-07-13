using Robust.Shared.GameObjects;

namespace Content.Shared.Tabletop.Components
{
    /**
     * <summary>
     * Allows an entity to be dragged around by the mouse. The position is updated for all player while dragging.
     * </summary>
     */
    [RegisterComponent]
    public class TabletopDraggableComponent : Component
    {
        public override string Name => "TabletopDraggable";

        /**
         * <summary>
         * Decides whether this entity can be moved.
         * </summary>
         */
        public bool CanStartDrag()
        {
            // TODO: implement permissions; only allow specific users to move object (may or may not be necessery)

            return true;
        }
    }
}
