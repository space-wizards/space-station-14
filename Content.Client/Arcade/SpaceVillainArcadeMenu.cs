using System.Numerics;
using Content.Client.Arcade.UI;
using Content.Shared.Arcade;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Arcade
{
    public sealed class SpaceVillainArcadeMenu : DefaultWindow
    {
        private readonly Label _enemyNameLabel;
        private readonly Label _playerHPLabel;
        private readonly Label _playerMPLabel;
        private readonly Label _enemyHPLabel;
        private readonly Label _enemyMPLabel;
        private readonly Label _playerActionLabel;
        private readonly Label _enemyActionLabel;

        private readonly Button[] _gameButtons = new Button[3]; //used to disable/enable all game buttons

        public event Action<SharedSpaceVillainArcadeComponent.PlayerAction>? OnPlayerAction;

        public SpaceVillainArcadeMenu()
        {
            MinSize = new Vector2(540, 225);
            Title = Loc.GetString("spacevillain-menu-title");

            var grid = new GridContainer()
            {
                Columns = 1,
                HorizontalAlignment = HAlignment.Stretch,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Stretch,
                VerticalExpand = true,
            };

            var infoGrid = new GridContainer()
            {
                Columns = 3,
                HorizontalAlignment = HAlignment.Stretch,
                HorizontalExpand = true,
            };

            // [---PLAYER---] | [---ENEMY---]

            infoGrid.AddChild(new Label { Text = Loc.GetString("spacevillain-menu-label-player"), Align = Label.AlignMode.Center });
            infoGrid.AddChild(new Label { Text = "|", Align = Label.AlignMode.Center });
            _enemyNameLabel = new Label { Align = Label.AlignMode.Center };
            infoGrid.AddChild(_enemyNameLabel);

            // [-HP-] [-MP-] | [-HP-] [-MP-]

            var playerInfoGrid = new GridContainer()
            {
                Columns = 2,
                HorizontalAlignment = HAlignment.Stretch,
                HorizontalExpand = true,
            };
            _playerHPLabel = new Label { Align = Label.AlignMode.Center, Modulate = Color.SpringGreen, HorizontalExpand = true };
            _playerMPLabel = new Label { Align = Label.AlignMode.Center, Modulate = Color.Aquamarine, HorizontalExpand = true };
            playerInfoGrid.AddChild(_playerHPLabel);
            playerInfoGrid.AddChild(_playerMPLabel);
            infoGrid.AddChild(playerInfoGrid);

            infoGrid.AddChild(new Label { Text = "|", Align = Label.AlignMode.Center });

            var enemyInfoGrid = new GridContainer()
            {
                Columns = 2,
                HorizontalAlignment = HAlignment.Stretch,
                HorizontalExpand = true,
            };
            _enemyHPLabel = new Label { Align = Label.AlignMode.Center, Modulate = Color.Salmon, HorizontalExpand = true };
            _enemyMPLabel = new Label { Align = Label.AlignMode.Center, Modulate = Color.Aquamarine, HorizontalExpand = true };
            enemyInfoGrid.AddChild(_enemyHPLabel);
            enemyInfoGrid.AddChild(_enemyMPLabel);
            infoGrid.AddChild(enemyInfoGrid);

            grid.AddChild(infoGrid);

            // Vertical spacer
            grid.AddChild(new BoxContainer { VerticalExpand = true });

            // [PLAYER ACTION---]
            // [----ENEMY ACTION]

            _playerActionLabel = new Label { Align = Label.AlignMode.Left };
            grid.AddChild(_playerActionLabel);

            _enemyActionLabel = new Label { Align = Label.AlignMode.Right };
            grid.AddChild(_enemyActionLabel);

            // Vertical spacer
            grid.AddChild(new BoxContainer { VerticalExpand = true });

            // [--ATTACK-] [---HEAL---] [-RECHARGE-]
            var buttonGrid = new GridContainer { Columns = 3, HorizontalAlignment = HAlignment.Stretch, HorizontalExpand = true };
            _gameButtons[0] = new Button()
            {
                Text = Loc.GetString("spacevillain-menu-button-attack"),
                HorizontalExpand = true,
            };

            _gameButtons[0].OnPressed +=
                _ => OnPlayerAction?.Invoke(SharedSpaceVillainArcadeComponent.PlayerAction.Attack);
            buttonGrid.AddChild(_gameButtons[0]);

            _gameButtons[1] = new Button()
            {
                Text = Loc.GetString("spacevillain-menu-button-heal"),
                HorizontalExpand = true,
            };

            _gameButtons[1].OnPressed +=
                _ => OnPlayerAction?.Invoke(SharedSpaceVillainArcadeComponent.PlayerAction.Heal);
            buttonGrid.AddChild(_gameButtons[1]);

            _gameButtons[2] = new Button()
            {
                Text = Loc.GetString("spacevillain-menu-button-recharge"),
                HorizontalExpand = true,
            };

            _gameButtons[2].OnPressed +=
                _ => OnPlayerAction?.Invoke(SharedSpaceVillainArcadeComponent.PlayerAction.Recharge);
            buttonGrid.AddChild(_gameButtons[2]);

            grid.AddChild(buttonGrid);

            // [---NEW GAME---]

            var newGame = new Button()
            {
                Text = Loc.GetString("spacevillain-menu-button-new-game")
            };

            newGame.OnPressed += _ => OnPlayerAction?.Invoke(SharedSpaceVillainArcadeComponent.PlayerAction.NewGame);
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
            if (message is SharedSpaceVillainArcadeComponent.SpaceVillainArcadeMetaDataUpdateMessage metaMessage)
                UpdateMetadata(metaMessage);

            _playerHPLabel.Text = $"{message.PlayerHP} HP";
            _playerMPLabel.Text = $"{message.PlayerMP} MP";
            _enemyHPLabel.Text = $"{message.EnemyHP} HP";
            _enemyMPLabel.Text = $"{message.EnemyMP} MP";
            _playerActionLabel.Text = message.PlayerActionMessage;
            _enemyActionLabel.Text = message.EnemyActionMessage;
        }
    }
}
