using System;
using System.Collections.Generic;
using System.Data;
using Content.Client.GameObjects.Components.Wires;
using Content.Client.Interfaces.Chat;
using Content.Client.VendingMachines;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Client.GameObjects.Components
{
    public class WiresMenu : SS14Window
    {
        protected override Vector2? CustomSize => (300, 450);

        private List<ClientWire> _cachedWires;

        public WiresBoundUserInterface Owner { get; set; }

        private readonly VBoxContainer _rows;

        public WiresMenu()
        {
            Title = "Wires";
            _rows = new VBoxContainer();
            Contents.AddChild(_rows);
        }

        public void Populate(List<ClientWire> wiresList)
        {
            _cachedWires = wiresList;
            _rows.RemoveAllChildren();
            foreach (var entry in wiresList)
            {
                var container = new HBoxContainer();
                var newLabel = new Label()
                {
                    Text = $"{entry.Color.Name()}: ",
                    FontColorOverride = entry.Color,
                };
                container.AddChild(newLabel);

                var newButton = new Button()
                {
                    Text = "Pulse",
                };
                newButton.OnPressed += _ => Owner.PerformAction(entry.Guid, WiresAction.Pulse);
                container.AddChild(newButton);

                newButton = new Button()
                {
                    Text = entry.IsCut ? "Mend" : "Cut",
                };
                newButton.OnPressed += _ => Owner.PerformAction(entry.Guid, entry.IsCut ? WiresAction.Mend : WiresAction.Cut);
                container.AddChild(newButton);
                _rows.AddChild(container);
            }
        }

    }
}
