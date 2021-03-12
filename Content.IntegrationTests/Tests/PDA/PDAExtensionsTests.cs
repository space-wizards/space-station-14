using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.PDA;
using NUnit.Framework;
using Robust.Server.Player;

namespace Content.IntegrationTests.Tests.PDA
{
    public class PDAExtensionsTests : ContentIntegrationTest
    {
        [Test]
        public async Task PlayerGetIdComponent()
        {
            var (client, server) = await StartConnectedServerClientPair();

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            var sPlayerManager = server.ResolveDependency<IPlayerManager>();

            await server.WaitAssertion(() =>
            {
                var player = sPlayerManager.GetAllPlayers().Single().AttachedEntity;

                Assert.NotNull(player);

                // The player spawns with an id on by default
                Assert.NotNull(player.PlayerGetId());
                Assert.True(player.TryPlayerGetId(out var id));
                Assert.NotNull(id);

                // Remove id
                var inventory = player.GetComponent<InventoryComponent>();

                foreach (var slot in inventory.Slots)
                {
                    var item = inventory.GetSlotItem(slot);

                    if (item == null)
                    {
                        continue;
                    }

                    if (item.Owner.HasComponent<PDAComponent>())
                    {
                        inventory.ForceUnequip(slot);
                    }
                }

                // No id
                Assert.Null(player.PlayerGetId());
                Assert.False(player.TryPlayerGetId(out id));
                Assert.Null(id);
            });
        }
    }
}
