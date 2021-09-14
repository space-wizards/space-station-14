using Robust.Shared.GameObjects;

namespace Content.Shared.Verbs
{
    public interface IShowContextMenu : IComponent
    {
        bool ShowContextMenu(IEntity examiner);
    }
}
