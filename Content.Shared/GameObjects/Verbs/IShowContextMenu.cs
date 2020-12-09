using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Verbs
{
    public interface IShowContextMenu : IComponent
    {
        bool ShowContextMenu(IEntity examiner);
    }
}
