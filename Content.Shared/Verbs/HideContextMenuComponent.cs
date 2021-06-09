#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Verbs
{
    [RegisterComponent]
    public class HideContextMenuComponent : Component, IShowContextMenu
    {
        public override string Name => "HideContextMenu";

        public bool ShowContextMenu(IEntity examiner)
        {
            return false;
        }
    }
}
