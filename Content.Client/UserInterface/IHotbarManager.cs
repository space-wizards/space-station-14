using Content.Client.GameObjects.EntitySystems;

namespace Content.Client.UserInterface
{
    public interface IHotbarManager
    {
        void BindUse(HotbarAction action);
        void UnbindUse(HotbarAction action);
        void RemoveAction(HotbarAction action);
    }
}
