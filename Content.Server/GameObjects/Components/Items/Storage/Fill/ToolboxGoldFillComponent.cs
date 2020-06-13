using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Items.Storage.Fill
{
    [RegisterComponent]
    internal sealed class ToolboxGoldFillComponent : Component, IMapInit
    {
        public override string Name => "ToolboxGoldFill";

#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        void IMapInit.MapInit()
        {
            var storage = Owner.GetComponent<IStorageComponent>();
            var random = IoCManager.Resolve<IRobustRandom>();

            void Spawn(string prototype)
            {
                storage.Insert(_entityManager.SpawnEntity(prototype, Owner.Transform.GridPosition));
            }

            Spawn("GoldStack");
            Spawn("GoldStack");
            Spawn("GoldStack");
            Spawn("GoldStack");
            Spawn("GoldStack");

            Spawn(random.Prob(0.05f) ? "DrinkGoldenCup" : "GoldStack");
        }
    }
}
