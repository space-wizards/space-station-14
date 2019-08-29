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
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649
        protected override Vector2? CustomSize => (300, 450);
        public WiresBoundUserInterface Owner { get; set; }

        private readonly VBoxContainer _rows;

        public WiresMenu()
        {
            IoCManager.InjectDependencies(this); // TODO: Remove this and use DynamicTypeFactory?
            Title = _localizationManager.GetString("Wires");
            _rows = new VBoxContainer();
            Contents.AddChild(_rows);
        }

        public void Populate(List<ClientWire> wiresList)
        {
            _rows.RemoveAllChildren();
            foreach (var entry in wiresList)
            {
                var container = new HBoxContainer();
                var newLabel = new Label()
                {
                    Text = $"{_localizationManager.GetString(entry.Color.Name())}: ",
                    FontColorOverride = entry.Color,
                };
                container.AddChild(newLabel);

                var newButton = new Button()
                {
                    Text = _localizationManager.GetString("Pulse"),
                };
                newButton.OnPressed += _ => Owner.PerformAction(entry.Guid, WiresAction.Pulse);
                container.AddChild(newButton);

                newButton = new Button()
                {
                    Text = entry.IsCut ? _localizationManager.GetString("Mend") : _localizationManager.GetString("Cut"),
                };
                newButton.OnPressed += _ => Owner.PerformAction(entry.Guid, entry.IsCut ? WiresAction.Mend : WiresAction.Cut);
                container.AddChild(newButton);
                _rows.AddChild(container);
            }
        }

    }
}
