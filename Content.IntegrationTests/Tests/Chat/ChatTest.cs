#nullable enable

using System.Threading.Tasks;
using Content.Client.Chat;
using Content.Client.Interfaces.Chat;
using NUnit.Framework;
using Robust.Client.Interfaces.UserInterface;
using Robust.UnitTesting.Client.UserInterface;

namespace Content.IntegrationTests.Tests.Chat
{
    [TestFixture]
    // also tests client ChatManager but it's internal atm so we can't mark it
    [TestOf(typeof(ChatBox))]
    public class ChatTest : ContentIntegrationTest
    {
        [Test]
        public async Task UpdatesChannelSelectorAndFiltersWhenAdminStatusChangesTest()
        {
            var (client, server) = await StartConnectedServerClientPair();

            await server.WaitIdleAsync();
            await client.WaitIdleAsync();

            var clientChatMgr = client.ResolveDependency<IChatManager>();
            await client.WaitAssertion(() =>
            {
                // get the current ChatBox
                var curChatBox = clientChatMgr.CurrentChatBox;
                Assert.NotNull(curChatBox);

                // initially, we should see all the available starting channels in a particular order.
                var channelSelector =
                    curChatBox!.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0) as ChannelSelectorButton;
                Assert.NotNull(channelSelector);
                // default to OOC
                Assert.That(channelSelector!.Text, Is.EqualTo("OOC"));

                // click it to reveal the available channels
                //todo;


            });
        }
    }
}
