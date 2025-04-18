using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed class ServerListBox : BoxContainer
{
    private IGameController _gameController;
    private List<Button> _connectButtons = new();
    private IUriOpener _uriOpener;

    public ServerListBox()
    {
        _gameController = IoCManager.Resolve<IGameController>();
        _uriOpener = IoCManager.Resolve<IUriOpener>();
        Orientation = LayoutOrientation.Vertical;

        var scrollContainer = new ScrollContainer
        {
            HScrollEnabled = false,
            VScrollEnabled = true,
            MinHeight = 330,
            MaxHeight = 330,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        var serverContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        scrollContainer.AddChild(serverContainer);
        AddChild(scrollContainer);

        AddServers(serverContainer);
    }

    private void AddServers(BoxContainer container)
    {
        AddServerInfo(container, "Титан", "Сервер с упором на высокий уровень РП", "ss14://f2.deadspace14.net:1212", null);
        AddServerInfo(container, "Деймос", "Сервер с сбалансированным геймплеем", "ss14://f3.deadspace14.net:1213", null);
        AddServerInfo(container, "Союз-1", "Сервер в сеттинге станции СССП", "ss14://s1.deadspace14.net:1213", null);
        AddServerInfo(container, "Фронтир", "Сервер про космические путешествия и торговлю", "ss14://ff.deadspace14.net:1213", null);
        AddServerInfo(container, "Конфедерация", "Сервер с альтернативной сборкой", "ss14s://backmen.ru/ss14/main", null);
    }

    private void AddServerInfo(BoxContainer container, string serverName, string description, string serverUrl, string? discord)
    {
        var serverBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            MinHeight = 50,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var nameAndDescriptionBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
        };

        var serverNameLabel = new Label
        {
            Text = serverName,
            MinWidth = 200
        };

        var descriptionLabel = new RichTextLabel
        {
            MaxWidth = 500
        };
        descriptionLabel.SetMessage(FormattedMessage.FromMarkup(description));

        var buttonBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Right
        };

        var connectButton = new Button
        {
            Text = "Подключиться"
        };

        if (discord != null)
        {
            var discordButton = new Button
            {
                Text = "Discord"
            };

            discordButton.OnPressed += _ =>
            {
                _uriOpener.OpenUri(discord);
            };

            buttonBox.AddChild(discordButton);
        }

        _connectButtons.Add(connectButton);

        connectButton.OnPressed += _ =>
        {
            _gameController.Redial(serverUrl, "Connecting to another server...");

            foreach (var button in _connectButtons)
            {
                button.Disabled = true;
            }
        };

        buttonBox.AddChild(connectButton);

        nameAndDescriptionBox.AddChild(serverNameLabel);
        nameAndDescriptionBox.AddChild(descriptionLabel);

        serverBox.AddChild(nameAndDescriptionBox);
        serverBox.AddChild(buttonBox);

        container.AddChild(serverBox);
    }
}
