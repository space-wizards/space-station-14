
using Content.Client.GameObjects.Components.Cargo;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Cargo
{
    public class GalacticBankSelectionMenu : SS14Window
    {
        private readonly ItemList _accounts;
        private int _accountCount = 0;
        private string[] _accountNames = new string[] { };
        private int[] _accountIds = new int[] { };
        private int _selectedAccountId = -1;

        protected override Vector2? CustomSize => (300, 300);

        public CargoConsoleBoundUserInterface Owner;

        public GalacticBankSelectionMenu()
        {
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Galactic Bank Selection");

            _accounts = new ItemList() { SelectMode = ItemList.ItemListSelectMode.Single };

            var margin = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            margin.AddChild(_accounts);

            Contents.AddChild(margin);
        }

        public void Populate(int accountCount, string[] accountNames, int[] accountIds, int selectedAccountId)
        {
            _accountCount = accountCount;
            _accountNames = accountNames;
            _accountIds = accountIds;
            _selectedAccountId = selectedAccountId;

            _accounts.Clear();
            for (var i = 0; i < _accountCount; i++)
            {
                var id = _accountIds[i];
                _accounts.AddItem($"ID: {id} || {_accountNames[i]}");
                if (id == _selectedAccountId)
                    _accounts[id].Selected = true;
            }
        }
    }
}
