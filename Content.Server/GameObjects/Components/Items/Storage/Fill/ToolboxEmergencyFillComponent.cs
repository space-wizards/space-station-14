using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Items.Storage.Fill
{
    [RegisterComponent]
    internal sealed class ToolboxEmergencyFillComponent : Component, IMapInit
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Name => "ToolboxEmergencyFill";

        void IMapInit.MapInit()
        {
            var storage = Owner.GetComponent<IStorageComponent>();
            var random = IoCManager.Resolve<IRobustRandom>();

            void Spawn(string prototype)
            {
                storage.Insert(_entityManager.SpawnEntity(prototype, Owner.Transform.Coordinates));
            }

            Spawn("BreathMaskClothing");
            Spawn("BreathMaskClothing");
            Spawn("FoodChocolateBar");
            Spawn("FlashlightLantern");
            Spawn("FlashlightLantern");

            Spawn(random.Prob(0.15f) ? "HarmonicaInstrument" : "FoodChocolateBar");
        }
    }
}
