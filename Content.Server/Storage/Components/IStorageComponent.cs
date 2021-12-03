using Robust.Shared.GameObjects;

namespace Content.Server.Storage.Components
{
    public interface IStorageComponent : IComponent
    {
        bool Remove(IEntity entity);
        bool Insert(IEntity entity);
        bool CanInsert(IEntity entity);
    }
}
