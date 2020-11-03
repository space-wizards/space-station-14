using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Content.Shared.Kitchen;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public class ReagentGrinderBoundUserInterface : BoundUserInterface
    {

        private GrinderMenu _menu;

        public ReagentGrinderBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner,uiKey) { }

        protected override void Open()
        {
            base.Open();
            _menu = new GrinderMenu(this);
            _menu.OpenCentered();
            _menu.OnClose += OnMenuClose;
        }

        private void OnMenuClose()
        {
            //throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (!(state is ReagentGrinderInterfaceState cState))
            {
                return;
            }

            _menu.BeakerContentBox.EjectButton.Disabled = !cState.HasBeakerIn;

        }


        public class GrinderMenu : SS14Window
        {
            /*The contents of the chamber and beaker will both be vertical scroll rectangles.
             * Will have a vsplit to split the g/j buttons from the contents menu.
             * |--------------------------------\
             * |     |  Chamber [E]   Beaker [E] |
             * | [G] |  |     |       |     |    |
             * |     |  |     |       |     |    |
             * |     |  |     |       |     |    |
             * | [J] |  |-----|       |-----|    |
             * |     |                           |
             * \---------------------------------/
             * 
             */

            private ReagentGrinderBoundUserInterface Owner { get; set; }
            protected override Vector2? CustomSize => (512, 256);

            //We'll need 4 buttons, grind, juice, eject beaker, eject the chamber contents.
            public Button GrindButton { get; }
            public Button JuiceButton { get; }

            public Button EjectChamberButton { get; }
            public Button EjectBeakerButton { get; }


            public LabelledContentBox ChamberConentBox { get; }
            public LabelledContentBox BeakerContentBox { get; }

            public sealed class LabelledContentBox : VBoxContainer
            {
                public string LabelText { get; set; }
                public ItemList BoxContents { get; set; }

                public Button EjectButton { get; set; }

                private Label _label;

                public LabelledContentBox(string labelText, string buttonText) : base()
                {

                    _label = new Label
                    {
                        Text = labelText,
                        Align = Label.AlignMode.Center,
                    };

                    EjectButton = new Button
                    {
                        Text = buttonText,
                        TextAlign = Label.AlignMode.Center,


                    };

                    var vSplit = new HSplitContainer
                    {
                        Children =
                        {
                            _label,
                            EjectButton
                        }
                    };

                    AddChild(vSplit);
                    BoxContents = new ItemList
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        SelectMode = ItemList.ItemListSelectMode.Button,
                        SizeFlagsStretchRatio = 2,
                        CustomMinimumSize = (100, 128)
                    };
                    AddChild(BoxContents);


                }
            }

            public GrinderMenu(ReagentGrinderBoundUserInterface owner = null)
            {
                Owner = owner;
                Title = Loc.GetString("All-In-One Grinder 3000");

                var hSplit = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.Fill,
                    SizeFlagsVertical = SizeFlags.Fill
                };

                var vBoxGrindJuiceButtonPanel = new VBoxContainer
                {
                    SizeFlagsVertical = SizeFlags.ShrinkCenter
                };


                GrindButton = new Button
                {
                    Text = Loc.GetString("Grind"),
                    TextAlign = Label.AlignMode.Center,
                    CustomMinimumSize = (64,64)
                };

                JuiceButton = new Button
                {
                    Text = Loc.GetString("Juice"),
                    TextAlign = Label.AlignMode.Center,
                    CustomMinimumSize = (64, 64)
                };

                vBoxGrindJuiceButtonPanel.AddChild(GrindButton);
                //inner button padding
                vBoxGrindJuiceButtonPanel.AddChild(new Control
                {
                    CustomMinimumSize = (0,16),
                });
                vBoxGrindJuiceButtonPanel.AddChild(JuiceButton);

                ChamberConentBox = new LabelledContentBox(Loc.GetString("Chamber"), Loc.GetString("Eject Contents"))
                {
                    //Modulate = Color.Red,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,                   
                    SizeFlagsStretchRatio = 2,

                };

                BeakerContentBox = new LabelledContentBox(Loc.GetString("Beaker"), Loc.GetString("Eject Beaker"))
                {
                    //Modulate = Color.Blue,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsStretchRatio = 2,
                };


                hSplit.AddChild(vBoxGrindJuiceButtonPanel);

                //Padding between the g/j buttons panel and the itemlist boxes panel.
                hSplit.AddChild(new Control
                {
                    CustomMinimumSize = (16,0),
                });
                hSplit.AddChild(ChamberConentBox);

                //Padding between the two itemlists.
                hSplit.AddChild(new Control
                {
                    CustomMinimumSize = (8, 0),
                });
                hSplit.AddChild(BeakerContentBox);
                Contents.AddChild(hSplit);
            }
        }

    }
}
