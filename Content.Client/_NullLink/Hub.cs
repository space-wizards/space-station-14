using System.Linq;
using Content.Client._Starlight.UI;
using Content.Client.Eui;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.UserInterface.Controls;
using Content.Shared._NullLink;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.NewLife;
using JetBrains.Annotations;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._NullLink;

// It’s not finished, still needs a lot of info displayed, scroll support once more servers show up, max hub width, a hide button, etc.
// But I’m rushing it for the upstream, will finish it properly someday.
internal sealed class Hub : PanelContainer, IDisposable
{
    [Dependency] private readonly ILogManager _logs = default!;
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Dependency] private readonly IGameController _game = default!;

    private HubSystem _hub = default!;

    private ISawmill _sawmill = default!;

    private GridContainer _gridContainer = null!;
    private readonly Dictionary<string, Row> _rows = [];

    public Hub()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logs.GetSawmill("Hub");

        HorizontalAlignment = HAlignment.Left;
        VerticalAlignment = VAlignment.Bottom;
        HorizontalExpand = true;
        VerticalExpand = true;
        Margin = new Thickness(1);
        StyleClasses.Add("AngleRect");

        _gridContainer = new GridContainer
        {
            Columns = 4,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        AddChild(_gridContainer);

        // This crap throws a NullRef exception—what the hell, the Try method doesn’t even check for null,
        // and Init is private, so there’s no way to figure out what’s going on in there.
        //try
        //{
        //    if (_systemManager.TryGetEntitySystem<HubSystem>(out var hub))
        //    {
        //        _hub = hub;
        //        _hub.OnInitialized += OnHubInitialized;
        //        _hub.OnServersRemoved += OnServersRemoved;
        //        _hub.OnServerUpdated += OnServerUpdated;
        //        _hub.OnServerInfoUpdated += OnServerInfoUpdated;
        //    }
        //    else
        //        _systemManager.SystemLoaded += OnSystemLoaded;
        //}
        //catch (NullReferenceException)
        //{
        //    _systemManager.SystemLoaded += OnSystemLoaded;
        //}

        _systemManager.SystemLoaded += OnSystemLoaded;
    }

    private void OnSystemLoaded(object? sender, SystemChangedArgs e)
    {
        if (e.System is not HubSystem hubSystem)
            return;

        _hub = hubSystem;
        _hub.OnInitialized += OnHubInitialized;
        _hub.OnServersRemoved += OnServersRemoved;
        _hub.OnServerUpdated += OnServerUpdated;
        _hub.OnServerInfoUpdated += OnServerInfoUpdated;

        _systemManager.SystemLoaded -= OnSystemLoaded;
    }

    private void OnServerInfoUpdated(string key, NullLink.ServerInfo info)
    {
        if (_rows.TryGetValue(key, out var row))
        {
            row.Status.Text = $"[font size=10]{info?.GetStatus()}[/font]";
            row.Online.Text = $"[font size=10]{info?.Players}/{info?.MaxPlayers}[/font]";
        }
    }

    private void OnServerUpdated(string key, NullLink.Server server)
    {
        if (_rows.TryGetValue(key, out var row))
        {
            row.Title.Text = $"[font size=10]{server.Title}[/font]";
            row.Title.ToolTip = server.Description;
        }
        else
            AddRow(_hub.CurrentGameHostName, key, server);
    }

    private void OnHubInitialized()
    {
        _gridContainer.RemoveAllChildren();
        _rows.Clear();

        for (var i = 0; i < 4; i++)
        {
            var stripe = new StripeBack();
            stripe.AddChild(new RichTextLabel
            {
                Text = "-----",
                HorizontalAlignment = HAlignment.Center,
            });
            _gridContainer.AddChild(stripe);
        }

        var currentHostName = _hub.CurrentGameHostName;
        foreach (var server in _hub.Servers ?? [])
            AddRow(currentHostName, server.Key, server.Value);
    }

    private void AddRow(string currentHostName, string key, NullLink.Server server)
    {
        var serverInfo = _hub.ServerInfo?.GetValueOrDefault(key);

        Button connectButton = null!;

        if (currentHostName == server.Title)
        {
            connectButton = new Button
            {
                Text = "Connected",
                Disabled = true,
                TooltipDelay = 1f,
                ToolTip = server.Description
            };
        }
        else
        {
            connectButton = new Button
            {
                Text = "Connect",
                TooltipDelay = 1f,
                ToolTip = server.Description
            };
            connectButton.OnPressed += _ => _game.Redial(server.ConnectionString);
        }

        var row = new Row()
        {
            Title = new RichTextLabel
            {
                Text = $"[font size=10]{server.Title}[/font]",
                HorizontalExpand = true,
                ToolTip = server.Description,
                TooltipDelay = 1f,
                HorizontalAlignment = HAlignment.Center,
            },
            Status = new RichTextLabel
            {
                Text = $"[font size=10]{serverInfo?.GetStatus()}[/font]",
                HorizontalAlignment = HAlignment.Center,
            },
            Online = new RichTextLabel
            {
                Text = $"[font size=10]{serverInfo?.Players}/{serverInfo?.MaxPlayers}[/font]",
                HorizontalAlignment = HAlignment.Center,
            },
            Button = connectButton
        };

        _rows[key] = row;

        _gridContainer.AddChild(row.Title);
        _gridContainer.AddChild(row.Status);
        _gridContainer.AddChild(row.Online);
        _gridContainer.AddChild(row.Button);
    }

    private void OnServersRemoved(string key)
    {
        if (!_rows.TryGetValue(key, out var row))
            return;

        _gridContainer.RemoveChild(row.Title);
        _gridContainer.RemoveChild(row.Status);
        _gridContainer.RemoveChild(row.Online);
        _gridContainer.RemoveChild(row.Button);
    }

    private sealed class Row
    {
        public RichTextLabel Title { get; set; } = null!;
        public RichTextLabel Status { get; set; } = null!;
        public RichTextLabel Online { get; set; } = null!;
        public Button Button { get; set; } = null!;
    }
}