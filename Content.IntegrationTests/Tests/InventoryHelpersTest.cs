using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Inventory;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(InventoryHelpers))]
    public class InventoryHelpersTest : ContentIntegrationTest
    {
        [Test]
        public async Task SpawnItemInSlotTest()
        {
            var server = StartServerDummyTicker();

            IEntity human = null;
            InventoryComponent inventory = null;
            StunnableComponent stun = null;


            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                var entityMan = IoCManager.Resolve<IEntityManager>();

                human = entityMan.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                inventory = human.GetComponent<InventoryComponent>();
                stun = human.GetComponent<StunnableComponent>();

                // Can't do the test if this human doesn't have the slots for it.
                Assert.That(inventory.HasSlot(Slots.INNERCLOTHING));
                Assert.That(inventory.HasSlot(Slots.IDCARD));

                Assert.That(inventory.SpawnItemInSlot(Slots.INNERCLOTHING, "UniformJanitor", true));

                // Do we actually have the uniform equipped?
                Assert.That(inventory.TryGetSlotItem(Slots.INNERCLOTHING, out ItemComponent uniform));
                Assert.That(uniform.Owner.Prototype != null && uniform.Owner.Prototype.ID == "UniformJanitor");

                stun.Stun(1f);

                // Since the mob is stunned, they can't equip this.
                Assert.That(inventory.SpawnItemInSlot(Slots.IDCARD, "AssistantIDCard", true), Is.False);

                // Make sure we don't have the ID card equipped.
                Assert.That(inventory.TryGetSlotItem(Slots.IDCARD, out ItemComponent _), Is.False);

                // Let's try skipping the interaction check and see if it equips it!
                Assert.That(inventory.SpawnItemInSlot(Slots.IDCARD, "AssistantIDCard", false));
                Assert.That(inventory.TryGetSlotItem(Slots.IDCARD, out ItemComponent id));
                Assert.That(id.Owner.Prototype != null && id.Owner.Prototype.ID == "AssistantIDCard");
            });

            await server.WaitIdleAsync();
        }
    }
}
