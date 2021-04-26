using Content.Client.GameObjects.Components.Arcade;
using Content.Shared.GameObjects.Components.Arcade;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Content.Client.Arcade
{
    public class SpaceVillainArcadeMenu : SS14Window
    {
        public SpaceVillainArcadeBoundUserInterface Owner { get; set; }

        private readonly Label _enemyNameLabel;
        private readonly Label _playerInfoLabel;
        private readonly Label _enemyInfoLabel;
        private readonly Label _playerActionLabel;
        private readonly Label _enemyActionLabel;

        private readonly Button[] _gameButtons = new Button[3]; //used to disable/enable all game buttons
        public SpaceVillainArcadeMenu(SpaceVillainArcadeBoundUserInterface owner)
        {
            MinSize = SetSize = (300, 225);
            Title = Loc.GetString("spacevillain-title");
            Owner = owner;

            var grid = new GridContainer {Columns = 1};

            var infoGrid = new GridContainer {Columns = 3};
            infoGrid.AddChild(new Label{ Text = Loc.GetString("spacevillain-player"), Align = Label.AlignMode.Center });
            infoGrid.AddChild(new Label{ Text = "|", Align = Label.AlignMode.Center });
            _enemyNameLabel = new Label{ Align = Label.AlignMode.Center};
            infoGrid.AddChild(_enemyNameLabel);

            _playerInfoLabel = new Label {Align = Label.AlignMode.Center};
            infoGrid.AddChild(_playerInfoLabel);
            infoGrid.AddChild(new Label{ Text = "|", Align = Label.AlignMode.Center });
            _enemyInfoLabel = new Label {Align = Label.AlignMode.Center};
            infoGrid.AddChild(_enemyInfoLabel);
            var centerContainer = new CenterContainer();
            centerContainer.AddChild(infoGrid);
            grid.AddChild(centerContainer);

            _playerActionLabel = new Label {Align = Label.AlignMode.Center};
            grid.AddChild(_playerActionLabel);

            _enemyActionLabel = new Label {Align = Label.AlignMode.Center};
            grid.AddChild(_enemyActionLabel);

            var buttonGrid = new GridContainer {Columns = 3};
            _gameButtons[0] = new ActionButton(Owner, SharedSpaceVillainArcadeComponent.PlayerAction.Attack)
                {Text = Loc.GetString("spacevillain-action-attack")};
            buttonGrid.AddChild(_gameButtons[0]);

            _gameButtons[1] = new ActionButton(Owner, SharedSpaceVillainArcadeComponent.PlayerAction.Heal)
                {Text = Loc.GetString("spacevillain-action-heal")};
            buttonGrid.AddChild(_gameButtons[1]);

            _gameButtons[2] = new ActionButton(Owner, SharedSpaceVillainArcadeComponent.PlayerAction.Recharge)
                {Text = Loc.GetString("spacevillain-action-recharge")};
            buttonGrid.AddChild(_gameButtons[2]);

            centerContainer = new CenterContainer();
            centerContainer.AddChild(buttonGrid);
            grid.AddChild(centerContainer);

            var newGame = new ActionButton(Owner, SharedSpaceVillainArcadeComponent.PlayerAction.NewGame)
                {Text = Loc.GetString("spacevillain-new-game")};
            grid.AddChild(newGame);

            Contents.AddChild(grid);
        }

        private void UpdateMetadata(SharedSpaceVillainArcadeComponent.SpaceVillainArcadeMetaDataUpdateMessage message)
        {
            Title = message.GameTitle;
            _enemyNameLabel.Text = message.EnemyName;

            foreach (var gameButton in _gameButtons)
            {
                gameButton.Disabled = message.ButtonsDisabled;
            }
        }

        public void UpdateInfo(SharedSpaceVillainArcadeComponent.SpaceVillainArcadeDataUpdateMessage message)
        {
            if(message is SharedSpaceVillainArcadeComponent.SpaceVillainArcadeMetaDataUpdateMessage metaMessage) UpdateMetadata(metaMessage);

            _playerInfoLabel.Text = $"HP: {message.PlayerHP} MP: {message.PlayerMP}";
            _enemyInfoLabel.Text = $"HP: {message.EnemyHP} MP: {message.EnemyMP}";
            _playerActionLabel.Text = message.PlayerActionMessage;
            _enemyActionLabel.Text = message.EnemyActionMessage;
        }

        private class ActionButton : Button
        {
            private readonly SpaceVillainArcadeBoundUserInterface _owner;
            private readonly SharedSpaceVillainArcadeComponent.PlayerAction _playerAction;

            public ActionButton(SpaceVillainArcadeBoundUserInterface owner,SharedSpaceVillainArcadeComponent.PlayerAction playerAction)
            {
                _owner = owner;
                _playerAction = playerAction;
                OnPressed += Clicked;
            }

            private void Clicked(ButtonEventArgs e)
            {
                _owner.SendAction(_playerAction);
            }
        }
    }
}
