using Robust.Shared.GameObjects;

namespace Content.Shared.Tabletop.Components
{
    [RegisterComponent]
    public class TabletopDraggableComponent : Component, ITabletopDraggable
    {
        public override string Name => "TabletopDraggable";
    }
}
