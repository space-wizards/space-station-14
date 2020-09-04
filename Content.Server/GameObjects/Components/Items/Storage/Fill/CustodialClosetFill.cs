using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Items.Storage.Fill
{
    [RegisterComponent]
    internal sealed class CustodialClosetFillComponent : Component, IMapInit
    {
        public override string Name => "CustodialClosetFill";

        void IMapInit.MapInit()
        {
            var storage = Owner.GetComponent<IStorageComponent>();

            void Spawn(string prototype)
            {
                storage.Insert(Owner.EntityManager.SpawnEntity(prototype, Owner.Transform.Coordinates));
            }

            Spawn("MopItem");
            Spawn("MopBucket");
            Spawn("WetFloorSign");
            Spawn("WetFloorSign");
            Spawn("WetFloorSign");
            Spawn("TrashBag");
            Spawn("TrashBag");
        }
    }
}
