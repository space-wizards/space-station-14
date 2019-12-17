using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Items.Storage.Fill
{
    [RegisterComponent]
    internal sealed class MedkitFillComponent : Component, IMapInit
    {
        public override string Name => "MedkitFill";

#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        void IMapInit.MapInit()
        {
            var storage = Owner.GetComponent<IStorageComponent>();

            void Spawn(string prototype)
            {
                storage.Insert(_entityManager.SpawnEntityAt(prototype, Owner.Transform.GridPosition));
            }

            Spawn("Brutepack");
            Spawn("Brutepack");
            Spawn("Brutepack");
            Spawn("Ointment");
            Spawn("Ointment");
            Spawn("Ointment");
        }
    }
}
