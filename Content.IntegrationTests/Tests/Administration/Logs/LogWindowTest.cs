using System.Linq;
using Content.Client.Administration.UI;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Administration.UI.Logs;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Logs;
using Content.Shared.Database;

namespace Content.IntegrationTests.Tests.Administration.Logs;

public sealed class LogWindowTest : InteractionTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true, AdminLogsEnabled = true, DummyTicker = false };

    [Test]
    public async Task TestAdminLogsWindow()
    {
        // Generate a log and give the server enough ticks to flush it to DB before searching.
        var log = Server.Resolve<IAdminLogManager>();
        var guid = Guid.NewGuid();
        await Server.WaitPost(() => log.Add(LogType.Unknown, $"{SPlayer} test log 1: {guid}"));
        await RunTicks(10);

        // Click the admin button in the menu bar
        await ClickWidgetControl<GameTopMenuBar, MenuButton>(nameof(GameTopMenuBar.AdminButton));
        var adminWindow = GetWindow<AdminMenuWindow>();

        // Find and click the "open logs" button; give the EUI time to open on the client.
        Assert.That(TryGetControlFromChildren<CommandButton>(x => x.Command == OpenAdminLogsCommand.Cmd, adminWindow, out var btn));
        await ClickControl(btn!);
        await RunTicks(10);
        var logWindow = GetWindow<AdminLogsWindow>();

        // Find the log search field and refresh buttons
        var search = logWindow.Logs.LogSearch;
        var refresh = logWindow.Logs.SearchButton;
        var cont = logWindow.Logs.LogsContainer;

        // Search for the log we added earlier.
        await Client.WaitPost(() => search.Text = guid.ToString());
        await ClickControl(refresh);

        AdminLogLabel[] searchResult = [];
        for (var i = 0; i < 60 && searchResult.Length == 0; i++)
        {
            await RunTicks(5);
            searchResult = cont.Children.Where(x => x.Visible && x is AdminLogLabel).Cast<AdminLogLabel>().ToArray();
        }
        Assert.That(searchResult.Length, Is.EqualTo(1));
        Assert.That(searchResult[0].Log.Message, Contains.Substring($" test log 1: {guid}"));

        // Add a new log, flush to DB, then search for it.
        guid = Guid.NewGuid();
        await Server.WaitPost(() => log.Add(LogType.Unknown, $"{SPlayer} test log 2: {guid}"));
        await RunTicks(10);

        await Client.WaitPost(() => search.Text = guid.ToString());
        await ClickControl(refresh);

        searchResult = [];
        for (var i = 0; i < 60 && searchResult.Length == 0; i++)
        {
            await RunTicks(5);
            searchResult = cont.Children.Where(x => x.Visible && x is AdminLogLabel).Cast<AdminLogLabel>().ToArray();
        }
        Assert.That(searchResult.Length, Is.EqualTo(1));
        Assert.That(searchResult[0].Log.Message, Contains.Substring($" test log 2: {guid}"));
    }
}
