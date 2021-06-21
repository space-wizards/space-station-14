using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Research.UI
{
    public class ResearchClientServerSelectionMenu : SS14Window
    {
        private readonly ItemList _servers;
        private int _serverCount;
        private string[] _serverNames = System.Array.Empty<string>();
        private int[] _serverIds = System.Array.Empty<int>();
        private int _selectedServerId = -1;

        public ResearchClientBoundUserInterface Owner { get; }

        public ResearchClientServerSelectionMenu(ResearchClientBoundUserInterface owner)
        {
            MinSize = SetSize = (300, 300);
            IoCManager.InjectDependencies(this);

            Owner = owner;
            Title = Loc.GetString("research-client-server-selection-menu-title");

            _servers = new ItemList() {SelectMode = ItemList.ItemListSelectMode.Single};

            _servers.OnItemSelected += OnItemSelected;
            _servers.OnItemDeselected += OnItemDeselected;

            Contents.AddChild(_servers);
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
                _servers.AddItem(Loc.GetString("research-client-server-selection-menu-server-entry-text", ("id", id), ("serverName", _serverNames[i])));
                if (id == _selectedServerId)
                {
                    _servers[id].Selected = true;
                }
            }

            _servers.OnItemSelected += OnItemSelected;
            _servers.OnItemDeselected += OnItemDeselected;
        }
    }
}
