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

        public Button StartButton { get;}
        public Button EjectButton { get;}

        public GridContainer TimerButtons { get; }

        public ItemList IngredientsList { get;}

        public MicrowaveMenu(MicrowaveBoundUserInterface owner = null)
        {
            Owner = owner;
            Title = Loc.GetString("Microwave");
            var hSplit = new HSplitContainer
            {
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsVertical = SizeFlags.Fill
            };


            IngredientsList = new ItemList
            {
                SizeFlagsVertical = SizeFlags.Expand,
                SelectMode = ItemList.ItemListSelectMode.Button,
                SizeFlagsStretchRatio = 8,
                CustomMinimumSize = (100,100)
            };

            hSplit.AddChild(IngredientsList);

            var vSplit = new VSplitContainer();
            hSplit.AddChild(vSplit);

            var buttonGridContainer = new GridContainer
            {
                Columns = 2,
            };
            StartButton = new Button
            {
                Text = Loc.GetString("START"),
            };
            EjectButton = new Button
            {
                Text = Loc.GetString("EJECT CONTENTS"),
            };
            buttonGridContainer.AddChild(StartButton);
            buttonGridContainer.AddChild(EjectButton);
            vSplit.AddChild(buttonGridContainer);


            TimerButtons = new GridContainer
            {
                Columns = 5,

            };

            vSplit.AddChild(TimerButtons);


            Contents.AddChild(hSplit);


        }

      

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
