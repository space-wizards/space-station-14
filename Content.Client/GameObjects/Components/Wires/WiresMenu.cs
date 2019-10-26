using System.Collections.Generic;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Client.GameObjects.Components.Wires
{
    public class WiresMenu : SS14Window
    {
        private readonly ILocalizationManager _localizationManager;
        protected override Vector2? CustomSize => (300, 150);
        public WiresBoundUserInterface Owner { get; set; }

        private readonly VBoxContainer _wiresContainer;

        public WiresMenu(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
            Title = _localizationManager.GetString("Wires");
            _wiresContainer = new VBoxContainer();
            Contents.AddChild(_wiresContainer);
        }

        public void Populate(WiresBoundUserInterfaceState state)
        {
            _wiresContainer.RemoveAllChildren();
            foreach (var wire in state.WiresList)
            {
                var container = new HBoxContainer();
                var newLabel = new Label()
                {
                    Text = $"{_localizationManager.GetString(wire.Color.Name())}: ",
                    FontColorOverride = wire.Color,
                };
                container.AddChild(newLabel);

                var newButton = new Button()
                {
                    Text = _localizationManager.GetString("Pulse"),
                };
                newButton.OnPressed += _ => Owner.PerformAction(wire.Guid, WiresAction.Pulse);
                container.AddChild(newButton);

                newButton = new Button()
                {
                    Text = wire.IsCut ? _localizationManager.GetString("Mend") : _localizationManager.GetString("Cut"),
                };
                newButton.OnPressed += _ => Owner.PerformAction(wire.Guid, wire.IsCut ? WiresAction.Mend : WiresAction.Cut);
                container.AddChild(newButton);
                _wiresContainer.AddChild(container);
            }

            foreach (var status in state.Statuses)
            {
                var container = new HBoxContainer();
                container.AddChild(new Label
                {
                    Text = status
                });
                _wiresContainer.AddChild(container);
            }
        }

    }
}
