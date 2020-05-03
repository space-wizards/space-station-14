using System.Collections.Generic;
using Content.Shared.Chemistry;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public class MicrowaveMenu : SS14Window
    {
        protected override Vector2? CustomSize => (512, 256);

        private MicrowaveBoundUserInterface Owner { get; set; }

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
                Label = { Text = Loc.GetString("START"), FontColorOverride = Color.Green}
            };
            var ejectButton = new Button()
            {
                Label = { Text = Loc.GetString("EJECT REAGENTS"),FontColorOverride = Color.Red}
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
            startButton.OnPressed += args => Owner.Cook();
            ejectButton.OnPressed += args => Owner.Eject();

        }

        public void RefreshContentsDisplay(List<Solution.ReagentQuantity> reagents, List<EntityUid> solids)
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
                var name = IoCManager.Resolve<IEntityManager>().GetEntity(item).Prototype.Name;
                var solidButton = new Button()
                {
                    Text = $"{name}"
                };

                solidButton.OnPressed += args => Owner.EjectSolidWithIndex(solids.IndexOf(item));
                InnerScrollContainer.AddChild(solidButton);
            }

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            InnerScrollContainer.Dispose();
        }
    }
}
