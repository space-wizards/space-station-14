using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    public interface IStorageComponent
    {
        bool Remove(IEntity entity);
        bool Insert(IEntity entity);
        bool CanInsert(IEntity entity);
    }
}
