using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Research
{
    public class ResearchClientServerSelectionMenu : SS14Window
    {
        private ItemList _servers;
        private int _serverCount = 0;
        private string[] _serverNames = new string[]{};
        private int[] _serverIds = new int[]{};
        private int _selectedServerId = -1;

        protected override Vector2? CustomSize => (300, 300);
        public ResearchClientBoundUserInterface Owner { get; set; }

        public ResearchClientServerSelectionMenu()
        {
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Research Server Selection");

            _servers = new ItemList() {SelectMode = ItemList.ItemListSelectMode.Single};

            _servers.OnItemSelected += OnItemSelected;
            _servers.OnItemDeselected += OnItemDeselected;

            var margin = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                /*MarginTop = 5f,
                MarginLeft = 5f,
                MarginRight = -5f,
                MarginBottom = -5f,*/
            };

            margin.AddChild(_servers);

            Contents.AddChild(margin);
        }

        public void OnItemSelected(ItemList.ItemListSelectedEventArgs itemListSelectedEventArgs)
        {
            Owner.SelectServer(_serverIds[itemListSelectedEventArgs.ItemIndex]);
        }

        public void OnItemDeselected(ItemList.ItemListDeselectedEventArgs itemListDeselectedEventArgs)
        {
            Owner.DeselectServer();
        }

        public void Populate(int serverCount, string[] serverNames, int[] serverIds, int selectedServerId)
        {
            _serverCount = serverCount;
            _serverNames = serverNames;
            _serverIds = serverIds;
            _selectedServerId = selectedServerId;

            // Disable so we can select the new selected server without triggering a new sync request.
            _servers.OnItemSelected -= OnItemSelected;
            _servers.OnItemDeselected -= OnItemDeselected;

            _servers.Clear();
            for (var i = 0; i < _serverCount; i++)
            {
                var id = _serverIds[i];
                _servers.AddItem($"ID: {id} || {_serverNames[i]}");
                if (id == _selectedServerId)
                    _servers[id].Selected = true;
            }

            _servers.OnItemSelected += OnItemSelected;
            _servers.OnItemDeselected += OnItemDeselected;
        }
    }
}
