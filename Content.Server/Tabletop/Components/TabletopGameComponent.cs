using Robust.Shared.GameObjects;

namespace Content.Server.Tabletop.Components
{
    /// <summary>
    /// A component that makes an object playable as a tabletop game.
    /// </summary>
    [RegisterComponent]
    public class TabletopGameComponent : Component
    {
        public override string Name => "TabletopGame";
    }
}
