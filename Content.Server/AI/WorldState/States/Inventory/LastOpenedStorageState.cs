using Content.Server.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server.AI.WorldState.States.Inventory
{
    /// <summary>
    /// If we open a storage locker than it will be stored here
    /// Useful if we want to close it after
    /// </summary>
    public sealed class LastOpenedStorageState : StoredStateData<EntityUid>
    {
        // TODO: IF we chain lockers need to handle it.
        // Fine for now I guess
        public override string Name => "LastOpenedStorage";

        public override void SetValue(EntityUid value)
        {
            base.SetValue(value);
            if (value.Valid && !IoCManager.Resolve<IEntityManager>().HasComponent<EntityStorageComponent>(value))
            {
                Logger.Warning("Set LastOpenedStorageState for an entity that doesn't have a storage component");
            }
        }
    }
}
