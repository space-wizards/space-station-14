using Content.Client.GameObjects.Components.Arcade;
using Content.Shared.GameObjects.Components.Arcade;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Content.Client.Arcade
{
    public class SpaceVillainArcadeMenu : SS14Window
    {
        protected override Vector2? CustomSize => (400, 200);
        public SpaceVillainArcadeBoundUserInterface Owner { get; set; }

        private Label _enemyNameLabel;
        private Label _playerInfoLabel;
        private Label _enemyInfoLabel;
        private Label _playerActionLabel;
        private Label _enemyActionLabel;
        public SpaceVillainArcadeMenu(SpaceVillainArcadeBoundUserInterface owner)
        {
            Title = "Space Villain";
            Owner = owner;

            GridContainer grid = new GridContainer();
            grid.Columns = 1;

            GridContainer infoGrid = new GridContainer();
            infoGrid.Columns = 3;
            infoGrid.AddChild(new Label{ Text = "Player", Align = Label.AlignMode.Center });
            infoGrid.AddChild(new Label{ Text = "|", Align = Label.AlignMode.Center });
            _enemyNameLabel = new Label{ Align = Label.AlignMode.Center};
            infoGrid.AddChild(_enemyNameLabel);

            _playerInfoLabel = new Label {Align = Label.AlignMode.Center};
            infoGrid.AddChild(_playerInfoLabel);
            infoGrid.AddChild(new Label{ Text = "|", Align = Label.AlignMode.Center });
            _enemyInfoLabel = new Label {Align = Label.AlignMode.Center};
            infoGrid.AddChild(_enemyInfoLabel);
            CenterContainer centerContainer = new CenterContainer();
            centerContainer.AddChild(infoGrid);
            grid.AddChild(centerContainer);

            _playerActionLabel = new Label();
            _playerActionLabel.Align = Label.AlignMode.Center;
            grid.AddChild(_playerActionLabel);

            _enemyActionLabel = new Label();
            _enemyActionLabel.Align = Label.AlignMode.Center;
            grid.AddChild(_enemyActionLabel);

            GridContainer buttonGrid = new GridContainer();
            buttonGrid.Columns = 3;
            Button attack = new ActionButton(Owner, SharedSpaceVillainArcadeComponent.PlayerAction.Attack);
            attack.Text = "ATTACK";
            buttonGrid.AddChild(attack);

            Button heal = new ActionButton(Owner, SharedSpaceVillainArcadeComponent.PlayerAction.Heal);
            heal.Text = "HEAL";
            buttonGrid.AddChild(heal);

            Button recharge = new ActionButton(Owner, SharedSpaceVillainArcadeComponent.PlayerAction.Recharge);
            recharge.Text = "RECHARGE";
            buttonGrid.AddChild(recharge);

            centerContainer = new CenterContainer();
            centerContainer.AddChild(buttonGrid);
            grid.AddChild(centerContainer);

            Button newGame = new ActionButton(Owner, SharedSpaceVillainArcadeComponent.PlayerAction.NewGame);
            newGame.Text = "New Game";
            grid.AddChild(newGame);

            centerContainer = new CenterContainer();
            centerContainer.AddChild(grid);
            Contents.AddChild(centerContainer);
        }

        private void UpdateMetadata(SharedSpaceVillainArcadeComponent.SpaceVillainArcadeMetaDataUpdateMessage message)
        {
            Title = message.GameTitle;
            _enemyNameLabel.Text = message.EnemyName;
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
            private SpaceVillainArcadeBoundUserInterface _owner;
            private SharedSpaceVillainArcadeComponent.PlayerAction _playerAction;

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
