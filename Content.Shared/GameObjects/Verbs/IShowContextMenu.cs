#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Verbs
{
    public interface IShowContextMenu : IComponent
    {
        bool ShowContextMenu(IEntity examiner);
    }
}
