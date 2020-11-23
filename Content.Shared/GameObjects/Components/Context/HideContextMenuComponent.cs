using Content.Shared.GameObjects.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Context
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
