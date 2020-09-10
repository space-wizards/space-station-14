using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Items.Storage.Fill
{
    [RegisterComponent]
    internal sealed class ToolboxElectricalFillComponent : Component, IMapInit
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Name => "ToolboxElectricalFill";

        void IMapInit.MapInit()
        {
            var storage = Owner.GetComponent<IStorageComponent>();
            var random = IoCManager.Resolve<IRobustRandom>();

            void Spawn(string prototype)
            {
                storage.Insert(_entityManager.SpawnEntity(prototype, Owner.Transform.Coordinates));
            }

            Spawn("Screwdriver");
            Spawn("Crowbar");
            Spawn("Wirecutter");
            Spawn("ApcExtensionCableStack");
            Spawn("MVWireStack");
            Spawn(random.Prob(0.05f) ? "GlovesYellow" : "HVWireStack");
        }
    }
}
