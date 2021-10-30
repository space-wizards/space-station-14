using Robust.Shared.GameObjects;

namespace Content.Server.Storage.Components
{
    public interface IStorageComponent
    {
        bool Remove(IEntity entity);
        bool Insert(IEntity entity);
        bool CanInsert(IEntity entity);
    }
}
