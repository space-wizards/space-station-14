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

        private Label _infoLabel;
        private Label _playerActionLabel;
        private Label _enemyActionLabel;
        public SpaceVillainArcadeMenu(SpaceVillainArcadeBoundUserInterface owner)
        {
            Title = "Space Villain";
            Owner = owner;

            GridContainer grid = new GridContainer();
            grid.Columns = 1;

            //maybe make this dynamic somehow
            _infoLabel = new Label();
            CenterContainer centerContainer = new CenterContainer();
            centerContainer.AddChild(_infoLabel);
            grid.AddChild(centerContainer);

            _playerActionLabel = new Label();
            centerContainer = new CenterContainer();
            centerContainer.AddChild(_playerActionLabel);
            grid.AddChild(centerContainer);

            _enemyActionLabel = new Label();
            centerContainer = new CenterContainer();
            centerContainer.AddChild(_enemyActionLabel);
            grid.AddChild(centerContainer);
            //----

            //make this dynamic somehow
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
            //---

            Button newGame = new ActionButton(Owner, SharedSpaceVillainArcadeComponent.PlayerAction.NewGame);
            newGame.Text = "New Game";
            grid.AddChild(newGame);

            centerContainer = new CenterContainer();
            centerContainer.AddChild(grid);
            Contents.AddChild(centerContainer);
        }

        public void UpdateInfo(int player_hp, int player_mp, int enemy_hp, int enemy_mp, string player_action, string enemy_action)
        {
            _infoLabel.Text = $"HP:{player_hp} MP:{player_mp} | HP:{enemy_hp} MP:{enemy_mp}";
            _playerActionLabel.Text = player_action;
            _enemyActionLabel.Text = enemy_action;
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
