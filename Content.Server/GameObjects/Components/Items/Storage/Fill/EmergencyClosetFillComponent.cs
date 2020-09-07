using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Items.Storage.Fill
{
    [RegisterComponent]
    internal sealed class EmergencyClosetFillComponent : Component, IMapInit
    {
        public override string Name => "EmergencyClosetFill";

        void IMapInit.MapInit()
        {
            var storage = Owner.GetComponent<IStorageComponent>();
            var random = IoCManager.Resolve<IRobustRandom>();

            void Spawn(string prototype)
            {
                storage.Insert(Owner.EntityManager.SpawnEntity(prototype, Owner.Transform.Coordinates));
            }

            if (random.Prob(0.4f))
            {
                Spawn("ToolboxEmergencyFilled");
            }

            var pick = random.Next(0, 100);
            if (pick < 40) // 40%
            {
                // TODO: uncomment when we actually have these items.
                // Spawn("TankOxygenSmallFilled");
                // Spawn("TankOxygenSmallFilled");
                Spawn("BreathMaskClothing");
                Spawn("BreathMaskClothing");
            }
            else if (pick < 65) // 25%
            {
                // Spawn("TankOxygenSmallFilled");
                // Spawn("MedkitOxygenFilled");
                Spawn("BreathMaskClothing");
            }
            else if (pick < 85) // 20%
            {
                // Spawn("TankOxygenFilled");
                Spawn("BreathMaskClothing");
            }
            else if (pick < 95) // 10%
            {
                // Spawn("TankOxygenSmallFilled");
                Spawn("BreathMaskClothing");
            }
            else if (pick < 99) // 4%
            {
                // nothing, doot
            }
            else // 1%
            {
                // teehee
                Owner.Delete();
            }
        }
    }
}
