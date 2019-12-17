using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Items.Storage.Fill
{
    [RegisterComponent]
    internal sealed class UtilityBeltClothingFillComponent : Component, IMapInit
    {
        public override string Name => "UtilityBeltClothingFill";

#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

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
            Spawn("CableStack");
        }
    }
}
