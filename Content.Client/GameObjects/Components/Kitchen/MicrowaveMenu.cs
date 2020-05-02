using System.Collections.Generic;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Content.Shared.Kitchen;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public class MicrowaveMenu : SS14Window
    {
        protected override Vector2? CustomSize => (512, 256);

        public MicrowaveBoundUserInterface Owner { get; set; }

        private List<Solution.ReagentQuantity> _heldReagents;

        private VBoxContainer InnerScrollContainer { get; set; }

        public MicrowaveMenu(MicrowaveBoundUserInterface owner = null)
        {
            Owner = owner;
            _heldReagents = new List<Solution.ReagentQuantity>();
            Title = Loc.GetString("Microwave");
            var vbox = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.Fill
            };

            var startButton = new Button()
            {
                Label = { Text = Loc.GetString("START")}
            };
            var ejectButton = new Button()
            {
                Label = { Text = Loc.GetString("EJECT REAGENTS")}
            };
            var scrollContainer = new ScrollContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand
            };

            InnerScrollContainer = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand
            };

            scrollContainer.AddChild(InnerScrollContainer);
            vbox.AddChild(startButton);
            vbox.AddChild(ejectButton);
            vbox.AddChild(scrollContainer);
            Contents.AddChild(vbox);
            startButton.OnPressed += OnCookButtonPressed;
            ejectButton.OnPressed += OnEjectButtonPressed;

        }

        private void OnEjectButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            Owner.Eject();
        }

        private void OnCookButtonPressed(BaseButton.ButtonEventArgs args)
        {
            Owner.Cook();

        }


        public void RefreshContents(List<Solution.ReagentQuantity> reagents, Dictionary<string,int> solids)
        {
            InnerScrollContainer.RemoveAllChildren();
            foreach (var item in reagents)
            {
                IoCManager.Resolve<IPrototypeManager>().TryIndex(item.ReagentId, out ReagentPrototype proto);

                InnerScrollContainer.AddChild(new Label()
                {

                    Text = $"{item.Quantity} {proto.Name}"
                });
            }

            foreach (var item in solids)
            {
                IoCManager.Resolve<IPrototypeManager>().TryIndex(item.Key, out EntityPrototype proto);
                var solidLabel = new Button()
                {
                    Text = $"{item.Value} {proto.Name}"
                };

                InnerScrollContainer.AddChild(solidLabel);
            }

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            InnerScrollContainer.Dispose();
        }
    }
}
