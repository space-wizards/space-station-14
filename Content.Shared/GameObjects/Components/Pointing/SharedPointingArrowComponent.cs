using Content.Shared.GameObjects.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Pointing
{
    public class SharedPointingArrowComponent : Component, IShowContextMenu
    {
        public sealed override string Name => "PointingArrow";

        public bool ShowContextMenu(IEntity examiner)
        {
            return false;
        }
    }
}
