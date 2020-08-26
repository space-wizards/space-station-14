using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Items.Storage.Fill
{
    [RegisterComponent]
    internal sealed class UtilityBeltClothingFillComponent : Component, IMapInit
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Name => "UtilityBeltClothingFill";

        void IMapInit.MapInit()
        {
            var storage = Owner.GetComponent<IStorageComponent>();

            void Spawn(string prototype)
            {
                storage.Insert(_entityManager.SpawnEntity(prototype, Owner.Transform.GridPosition));
            }

            Spawn("Crowbar");
            Spawn("Wrench");
            Spawn("Screwdriver");
            Spawn("Wirecutter");
            Spawn("Welder");
            Spawn("Multitool");
            Spawn("ApcExtensionCableStack");
        }
    }
}
