using Content.Server.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.AI.WorldState.States.Inventory
{
    /// <summary>
    /// If we open a storage locker than it will be stored here
    /// Useful if we want to close it after
    /// </summary>
    public sealed class LastOpenedStorageState : StoredStateData<IEntity>
    {
        // TODO: IF we chain lockers need to handle it.
        // Fine for now I guess
        public override string Name => "LastOpenedStorage";

        public override void SetValue(IEntity? value)
        {
            base.SetValue(value);
            if (value != null && !value.HasComponent<EntityStorageComponent>())
            {
                Logger.Warning("Set LastOpenedStorageState for an entity that doesn't have a storage component");
            }
        }
    }
}
