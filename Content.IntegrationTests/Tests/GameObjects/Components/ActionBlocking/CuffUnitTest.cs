#nullable enable

using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Content.Server.GameObjects.Components.ActionBlocking;
using System.Linq;
using Content.Server.GameObjects.Components.Body;
using Content.Shared.Body.Part;
using Content.Shared.GameObjects.Components.Body;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Robust.Shared.Prototypes;
using Content.Server.Body;
using Content.Client.GameObjects.Components.Items;

namespace Content.IntegrationTests.Tests.GameObjects.Components.ActionBlocking
{
    [TestFixture]
    [TestOf(typeof(CuffableComponent))]
    [TestOf(typeof(HandcuffComponent))]
    public class CuffUnitTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();

            IEntity human;
            IEntity otherHuman;
            IEntity cuffs;
            IEntity cables;
            HandcuffComponent cableHandcuff;
            HandcuffComponent handcuff;
            CuffableComponent cuffed;
            IHandsComponent hands;
            BodyManagerComponent body;

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                // Spawn the entities
                human = entityManager.SpawnEntity("BaseHumanMob_Content", MapCoordinates.Nullspace);
                otherHuman = entityManager.SpawnEntity("BaseHumanMob_Content", MapCoordinates.Nullspace);
                cuffs = entityManager.SpawnEntity("Handcuffs", MapCoordinates.Nullspace);
                cables = entityManager.SpawnEntity("Cablecuffs", MapCoordinates.Nullspace);

                human.Transform.WorldPosition = otherHuman.Transform.WorldPosition;

                // Test for components existing
                Assert.True(human.TryGetComponent(out cuffed!), $"Human has no {nameof(CuffableComponent)}");
                Assert.True(human.TryGetComponent(out hands!), $"Human has no {nameof(HandsComponent)}");
                Assert.True(human.TryGetComponent(out body!), $"Human has no {nameof(BodyManagerComponent)}");
                Assert.True(cuffs.TryGetComponent(out handcuff!), $"Handcuff has no {nameof(HandcuffComponent)}");
                Assert.True(cables.TryGetComponent(out cableHandcuff!), $"Cablecuff has no {nameof(HandcuffComponent)}");

                // Test to ensure cuffed players register the handcuffs
                cuffed.AddNewCuffs(cuffs);
                Assert.True(cuffed.CuffedHandCount > 0, "Handcuffing a player did not result in their hands being cuffed");

                // Test to ensure a player with 4 hands will still only have 2 hands cuffed
                AddHand(body);
                AddHand(body);
                Assert.True(cuffed.CuffedHandCount == 2 && hands.Hands.Count() == 4, "Player doesn't have correct amount of hands cuffed");

                // Test to give a player with 4 hands 2 sets of cuffs
                cuffed.AddNewCuffs(cables);
                Assert.True(cuffed.CuffedHandCount == 4, "Player doesn't have correct amount of hands cuffed");

            });

            await server.WaitIdleAsync();
        }

        private void AddHand(BodyManagerComponent body)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.TryIndex("bodyPart.LHand.BasicHuman", out BodyPartPrototype prototype);

            var part = new BodyPart(prototype);
            var slot = part.GetHashCode().ToString();

            body.Template.Slots.Add(slot, BodyPartType.Hand);
            body.TryAddPart(slot, part, true);
        }
    }
}
