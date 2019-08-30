using System;
using Content.Shared.GameObjects.Components.Research;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Research
{
    public class ResearchClientServerSelectionMenu : SS14Window
    {
        private ItemList _servers;

        protected override Vector2? CustomSize => (300, 300);
        public ResearchClientBoundUserInterface Owner { get; set; }

        public ResearchClientServerSelectionMenu()
        {
            Title = "Research Server Selection";

            _servers = new ItemList() {SelectMode = ItemList.ItemListSelectMode.Single};

            _servers.OnItemSelected += OnItemSelected;
            _servers.OnItemDeselected += OnItemDeselected;

            var margin = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                MarginTop = 5f,
                MarginLeft = 5f,
                MarginRight = -5f,
                MarginBottom = -5f,
            };

            margin.AddChild(_servers);

            Contents.AddChild(margin);
        }

        public void OnItemSelected(ItemList.ItemListSelectedEventArgs itemListSelectedEventArgs)
        {
            Owner.SelectServer(Owner.ServerIds[itemListSelectedEventArgs.ItemIndex]);
        }

        public void OnItemDeselected(ItemList.ItemListDeselectedEventArgs itemListDeselectedEventArgs)
        {
            Owner.DeselectServer();
        }

        public void Populate()
        {
            // Disable so we can select the new selected server without triggering a new sync request.
            _servers.OnItemSelected -= OnItemSelected;
            _servers.OnItemDeselected -= OnItemDeselected;

            _servers.Clear();
            for (var i = 0; i < Owner.ServerCount; i++)
            {
                var id = Owner.ServerIds[i];
                _servers.AddItem($"ID: {id} || {Owner.ServerNames[i]}");
                if(id == Owner.SelectedServerId)
                    _servers.Select(i);
            }

            _servers.OnItemSelected += OnItemSelected;
            _servers.OnItemDeselected += OnItemDeselected;
        }
    }
}
