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
    }
}
