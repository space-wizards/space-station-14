using Content.Shared.GameObjects.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Drag
{
    [RegisterComponent]
    public class DragShadowComponent : Component, IShowContextMenu
    {
        public override string Name => "DragShadow";

        public bool ShowContextMenu(IEntity examiner)
        {
            return false;
        }
    }
}
