#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Client.GameObjects.Components.Items;
using Content.Server.GameObjects.Components.ActionBlocking;
using Content.Server.GameObjects.Components.Body;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Body;
using NUnit.Framework;
using Robust.Server.Interfaces.Console;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.GameObjects.Components.ActionBlocking
{
    [TestFixture]
    [TestOf(typeof(CuffableComponent))]
    [TestOf(typeof(HandcuffComponent))]
    public class CuffUnitTest : ContentIntegrationTest
    {
        private const string PROTOTYPES = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Cuffable
  - type: Hands
  - type: Body
    template: HumanoidTemplate
    preset: HumanPreset
    centerSlot: torso

- type: entity
  name: HandcuffsDummy
  id: HandcuffsDummy
  components:
  - type: Handcuff
";
        [Test]
        public async Task Test()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = PROTOTYPES};
            var server = StartServerDummyTicker(options);

            IEntity human;
            IEntity otherHuman;
            IEntity cuffs;
            IEntity secondCuffs;
            HandcuffComponent handcuff;
            HandcuffComponent secondHandcuff;
            CuffableComponent cuffed;
            IHandsComponent hands;
            IBody body;

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                // Spawn the entities
                human = entityManager.SpawnEntity("HumanDummy", MapCoordinates.Nullspace);
                otherHuman = entityManager.SpawnEntity("HumanDummy", MapCoordinates.Nullspace);
                cuffs = entityManager.SpawnEntity("HandcuffsDummy", MapCoordinates.Nullspace);
                secondCuffs = entityManager.SpawnEntity("HandcuffsDummy", MapCoordinates.Nullspace);

                human.Transform.WorldPosition = otherHuman.Transform.WorldPosition;

                // Test for components existing
                Assert.True(human.TryGetComponent(out cuffed!), $"Human has no {nameof(CuffableComponent)}");
                Assert.True(human.TryGetComponent(out hands!), $"Human has no {nameof(HandsComponent)}");
                Assert.True(human.TryGetComponent(out body!), $"Human has no {nameof(IBody)}");
                Assert.True(cuffs.TryGetComponent(out handcuff!), $"Handcuff has no {nameof(HandcuffComponent)}");
                Assert.True(secondCuffs.TryGetComponent(out secondHandcuff!), $"Second handcuffs has no {nameof(HandcuffComponent)}");

                // Test to ensure cuffed players register the handcuffs
                cuffed.AddNewCuffs(cuffs);
                Assert.True(cuffed.CuffedHandCount > 0, "Handcuffing a player did not result in their hands being cuffed");

                // Test to ensure a player with 4 hands will still only have 2 hands cuffed
                AddHand(cuffed.Owner);
                AddHand(cuffed.Owner);
                Assert.True(cuffed.CuffedHandCount == 2 && hands.Hands.Count() == 4, "Player doesn't have correct amount of hands cuffed");

                // Test to give a player with 4 hands 2 sets of cuffs
                cuffed.AddNewCuffs(secondCuffs);
                Assert.True(cuffed.CuffedHandCount == 4, "Player doesn't have correct amount of hands cuffed");

            });

            await server.WaitIdleAsync();
        }

        private void AddHand(IEntity to)
        {
            var shell = IoCManager.Resolve<IConsoleShell>();
            shell.ExecuteCommand($"addhand {to.Uid}");
        }
    }
}
