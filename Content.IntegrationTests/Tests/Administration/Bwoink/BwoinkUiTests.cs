using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Content.Client.Administration.Managers;
using Content.Client.Administration.UI.BanList;
using Content.Client.Administration.UI.BanPanel;
using Content.Client.Administration.UI.Bwoink;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Administration.UI.PlayerPanel;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Managers.Bwoink;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Administration.Bwoink;

[TestFixture]
[TestOf(typeof(SharedBwoinkManager))]
public sealed class BwoinkUiTests : InteractionTest
{
    // TODO: Make this test not use the in-sim InteractionTest

    protected override PoolSettings Settings => new() {Connected = true, Dirty = true, AdminLogsEnabled = true, DummyTicker = false};

    private const string TestMessage = "Nik is a cat!";
    private const string TestUserName = "voidnerd";
    private static readonly ProtoId<BwoinkChannelPrototype> AhelpId = "AdminHelp";

    [TestPrototypes]
    private const string Prototypes = @"
- type: bwoinkChannel
  id: TestHelp1
  name: bwoink-channel-ahelp
  helpText: bwoink-channel-ahelp-help
  order: 1
  manageRequirement:
    requirements:
    - !type:AdminFlagRequirement
      flags: Adminhelp
  features:
  - !type:ManagerOnlyMessages
    prefix: bwoink-message-admin-only
    checkName: admin-ahelp-admin-only
  - !type:StatusMessages
";

    /// <summary>
    /// This tests checks if the status messages channel feature works.
    /// </summary>
    [Test]
    public async Task TestStatusMessages()
    {
        var cBwoinkManager = Client.ResolveDependency<ClientBwoinkManager>();
        var sBwoinkManager = Server.ResolveDependency<ServerBwoinkManager>();
        var sDbManager = Server.ResolveDependency<IServerDbManager>();

        // important, we do this here because GameTopMenuBar is not real inside the lobby
        var bwoinkWindow = await GetBwoinkWindow(cBwoinkManager, sBwoinkManager);

        await Client.ExecuteCommand("golobby");
        await RunTicks(5);
        Assert.That(Server.EntMan.System<GameTicker>().RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

        var dummySes = await Server.AddDummySession(TestUserName);
        await RunTicks(5);
        await sDbManager.UpdatePlayerRecordAsync(dummySes.UserId, dummySes.Name, IPAddress.Parse("83.240.187.182"), new ImmutableTypedHwid(ImmutableArray<byte>.Empty, HwidType.Legacy)); // the ipv4 address is random. no idea where it points to.

        bwoinkWindow.Channels.CurrentTab = 0; // Select the correct tab
        Assert.That(bwoinkWindow.Channels.Children[0], Is.TypeOf<BwoinkControl>()); // Check that the channel is manager
        var control = bwoinkWindow.Channels.Children[0] as BwoinkControl;
        foreach (var child in control!.ChannelSelector.PlayerListContainer.Children)
        {
            if (child is not ListContainerButton data)
                continue;

            var playerData = (PlayerListData)data.Data;
            if (playerData.Info.Username != dummySes.Name)
                continue;

            await ClickControl(child);
            break;
        }

        // We now *should* have selected an item in our channel selector. We verify by looking if we have a BwoinkPanel present.
        var panel = control.EnsurePanel(dummySes.UserId);
        Assert.That(panel.Visible, Is.True);

        panel.SenderLineEdit.Text = TestMessage;
        panel.SenderLineEdit.ForceSubmitText(); // needed since otherwise status messages wont be posted.
        await RunTicks(5);
        VerifyMessageCount(1, cBwoinkManager, sBwoinkManager, AhelpId, dummySes.UserId);

        // We disconnect the dummy session
        await Server.RemoveDummySession(dummySes);
        await RunTicks(5);
        VerifyMessageCount(2, cBwoinkManager, sBwoinkManager, AhelpId, dummySes.UserId);
        VerifyMessageSender(MessageBwoinkManager.BwoinkStatusTypes.Disconnect);

        // We re-add the dummy session
        dummySes = await Server.AddDummySession(TestUserName);
        await RunTicks(5);
        VerifyMessageCount(3, cBwoinkManager, sBwoinkManager, AhelpId, dummySes.UserId);
        VerifyMessageSender(MessageBwoinkManager.BwoinkStatusTypes.Reconnect);

        // Banning is just straight up not working. I have tried like
        // 5 DIFFERENT WAYS OF BANNING, NONE OF THEM ARE WORKING
        // FUCK YOU INTEGRATION TESTS.

        //VerifyMessageCount(4, cBwoinkManager, sBwoinkManager, AhelpId, dummySes.UserId);
        //VerifyMessageSender(MessageBwoinkManager.BwoinkStatusTypes.Banned);

        return;

        void VerifyMessageSender(MessageBwoinkManager.BwoinkStatusTypes type)
        {
            VerifyFor(GetLatestMessage(cBwoinkManager, AhelpId, dummySes.UserId));
            VerifyFor(GetLatestMessage(sBwoinkManager, AhelpId, dummySes.UserId));

            return;
            void VerifyFor(BwoinkMessage message) // crimes against humanity, a method within a method.
            {
                Assert.That(message.Flags.HasFlag(MessageFlags.System), Is.True); // Status messages should have a hidden sender.
                Assert.That(byte.TryParse(message.Sender, out var messageInt), Is.True);
                Assert.That(Enum.IsDefined(typeof(MessageBwoinkManager.BwoinkStatusTypes), messageInt), Is.True);
                Assert.That((MessageBwoinkManager.BwoinkStatusTypes)messageInt, Is.EqualTo(type));
            }
        }
    }

    /// <summary>
    /// This test checks to see if an un-adminned client can send a bwoink and if the server properly receives said bwoink.
    /// </summary>
    [Test]
    public async Task TestBwoinkClient()
    {
        // Dependencies:
        var cAdminManager = Client.ResolveDependency<IClientAdminManager>();
        var cBwoinkManager = Client.ResolveDependency<ClientBwoinkManager>();
        var sBwoinkManager = Server.ResolveDependency<ServerBwoinkManager>();

        // First, we un-admin ourselves
        await Client.ExecuteCommand("deadmin");
        await RunTicks(1);
        Assert.That(cAdminManager.IsAdmin(includeDeAdmin: false), Is.False);

        var bwoinkWindow = await GetBwoinkWindow(cBwoinkManager, sBwoinkManager);

        bwoinkWindow.Channels.CurrentTab = 0; // Select the correct tab
        Assert.That(bwoinkWindow.Channels.Children[0], Is.TypeOf<BwoinkPanel>()); // Check that the channel is non-manager
        var panel = bwoinkWindow.Channels.Children[0] as BwoinkPanel;
        // ReSharper disable once NullableWarningSuppressionIsUsed - The top assert checks for this.
        panel!.SenderLineEdit.SetText(TestMessage, true);
        panel.SenderLineEdit.ForceSubmitText();
        await RunTicks(5);

        Assert.Multiple(() =>
        {
            // Conversations should now have one message
            Assert.That(cBwoinkManager.Conversations.Values.Sum(x => x.Count), Is.EqualTo(1));
            Assert.That(sBwoinkManager.Conversations.Values.Sum(x => x.Count), Is.EqualTo(1));
        });

        Assert.That(Client.Session, Is.Not.Null);

        // Check if the message text we got matches what we sent:
#pragma warning disable NUnit2045 Explictily not doing that, as the second assert would KeyNotFound if the first fails.
        Assert.That(cBwoinkManager.Conversations[AhelpId].ContainsKey(Client.Session.UserId), Is.True);
        Assert.That(cBwoinkManager.Conversations[AhelpId][Client.Session.UserId].Messages, Has.Count.EqualTo(1));
#pragma warning restore NUnit2045

        var ourMessage = cBwoinkManager.Conversations[AhelpId][Client.Session.UserId].Messages[0];
        Assert.Multiple(() =>
        {
            Assert.That(ourMessage.Content, Is.EqualTo(TestMessage));
            Assert.That(ourMessage.SenderId, Is.Null); // The non-admin client should never receive the sender
            Assert.That(ourMessage.Flags.HasFlag(MessageFlags.NoReceivers), Is.True); // Our message does not have any receivers.
            Assert.That(ourMessage.Sender, Is.EqualTo(Client.Session.Name)); // We should be the sender
        });
    }

    /// <summary>
    /// Gets a bwoink window for the first time. Includes the check for any previous messages.
    /// </summary>
    private async Task<BwoinkWindow> GetBwoinkWindow(ClientBwoinkManager cBwoinkManager, ServerBwoinkManager sBwoinkManager)
    {
        Assert.Multiple(() =>
        {
            // Conversations should be empty
            Assert.That(cBwoinkManager.Conversations.Values.Sum(x => x.Count), Is.EqualTo(0));
            Assert.That(sBwoinkManager.Conversations.Values.Sum(x => x.Count), Is.EqualTo(0));
        });

        // Open ahelp
        await ClickWidgetControl<GameTopMenuBar, MenuButton>(nameof(GameTopMenuBar.AHelpButton));
        var bwoinkWindow = GetWindow<BwoinkWindow>();

        return bwoinkWindow;
    }

    /// <summary>
    /// Verifies that the message count we have is what we expect it to be for the specified user channel.
    /// If the userchannel is null we sum all channels.
    /// </summary>
    private void VerifyMessageCount(int count,
        ClientBwoinkManager cBwoinkManager,
        ServerBwoinkManager sBwoinkManager,
        ProtoId<BwoinkChannelPrototype> channel,
        NetUserId? user = null)
    {
        Assert.Multiple(() =>
        {
            Assert.That(cBwoinkManager.Conversations.ContainsKey(channel));
            Assert.That(sBwoinkManager.Conversations.ContainsKey(channel));
        });

        if (user.HasValue)
        {
            Assert.Multiple(() =>
            {
                Assert.That(cBwoinkManager.Conversations[channel].ContainsKey(user.Value));
                Assert.That(sBwoinkManager.Conversations[channel].ContainsKey(user.Value));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cBwoinkManager.Conversations[channel][user.Value].Messages, Has.Count.EqualTo(count));
                Assert.That(sBwoinkManager.Conversations[channel][user.Value].Messages, Has.Count.EqualTo(count));
            });
        }
        else
        {
            Assert.Multiple(() =>
            {
                Assert.That(cBwoinkManager.Conversations[channel].Values.Sum(x => x.Messages.Count), Is.EqualTo(count));
                Assert.That(sBwoinkManager.Conversations[channel].Values.Sum(x => x.Messages.Count), Is.EqualTo(count));
            });
        }
    }

    /// <summary>
    /// Gets the latest message within a channel / userChannel pair. Asserts when neither of those exist.
    /// </summary>
    private BwoinkMessage GetLatestMessage(SharedBwoinkManager manager,
        ProtoId<BwoinkChannelPrototype> channel,
        NetUserId userChannel)
    {
        Assert.That(manager.Conversations.ContainsKey(channel));
        Assert.That(manager.Conversations[channel].ContainsKey(userChannel));
        Assert.That(manager.Conversations[channel][userChannel].Messages, Has.Count.AtLeast(1));

        return manager.Conversations[channel][userChannel].Messages.Last();
    }
}
