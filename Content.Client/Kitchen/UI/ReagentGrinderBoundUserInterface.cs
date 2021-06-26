using System.Collections.Generic;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Shared.Chemistry.Solution.Solution;

namespace Content.Client.Kitchen.UI
{
    public class ReagentGrinderBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private GrinderMenu? _menu;
        private readonly Dictionary<int, EntityUid> _chamberVisualContents = new();
        private readonly Dictionary<int, ReagentQuantity> _beakerVisualContents = new();

        public ReagentGrinderBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            base.Open();

            _menu = new GrinderMenu(this);
            _menu.OpenCentered();
            _menu.OnClose += Close;
            _menu.GrindButton.OnPressed += args => SendMessage(new SharedReagentGrinderComponent.ReagentGrinderGrindStartMessage());
            _menu.JuiceButton.OnPressed += args => SendMessage(new SharedReagentGrinderComponent.ReagentGrinderJuiceStartMessage());
            _menu.ChamberContentBox.EjectButton.OnPressed += args => SendMessage(new SharedReagentGrinderComponent.ReagentGrinderEjectChamberAllMessage());
            _menu.BeakerContentBox.EjectButton.OnPressed += args => SendMessage(new SharedReagentGrinderComponent.ReagentGrinderEjectBeakerMessage());
            _menu.ChamberContentBox.BoxContents.OnItemSelected += args =>
            {
                SendMessage(new SharedReagentGrinderComponent.ReagentGrinderEjectChamberContentMessage(_chamberVisualContents[args.ItemIndex]));
            };

            _menu.BeakerContentBox.BoxContents.OnItemSelected += args =>
            {
                SendMessage(new SharedReagentGrinderComponent.ReagentGrinderVaporizeReagentIndexedMessage(_beakerVisualContents[args.ItemIndex]));
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            _chamberVisualContents?.Clear();
            _beakerVisualContents?.Clear();
            _menu?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (!(state is ReagentGrinderInterfaceState cState))
            {
                return;
            }

            if (_menu == null)
            {
                return;
            }

            _menu.BeakerContentBox.EjectButton.Disabled = !cState.HasBeakerIn;
            _menu.ChamberContentBox.EjectButton.Disabled = cState.ChamberContents.Length <= 0;
            _menu.GrindButton.Disabled = !cState.CanGrind || !cState.Powered;
            _menu.JuiceButton.Disabled = !cState.CanJuice || !cState.Powered;
            RefreshContentsDisplay(cState.ReagentQuantities, cState.ChamberContents, cState.HasBeakerIn);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (_menu == null)
            {
                return;
            }

            switch (message)
            {
                case SharedReagentGrinderComponent.ReagentGrinderWorkStartedMessage workStarted:
                    _menu.GrindButton.Disabled = true;
                    _menu.GrindButton.Modulate = workStarted.GrinderProgram == SharedReagentGrinderComponent.GrinderProgram.Grind ? Color.Green : Color.White;
                    _menu.JuiceButton.Disabled = true;
                    _menu.JuiceButton.Modulate = workStarted.GrinderProgram == SharedReagentGrinderComponent.GrinderProgram.Juice ? Color.Green : Color.White;
                    _menu.BeakerContentBox.EjectButton.Disabled = true;
                    _menu.ChamberContentBox.EjectButton.Disabled = true;
                    break;
                case SharedReagentGrinderComponent.ReagentGrinderWorkCompleteMessage doneMessage:
                    _menu.GrindButton.Disabled = false;
                    _menu.JuiceButton.Disabled = false;
                    _menu.GrindButton.Modulate = Color.White;
                    _menu.JuiceButton.Modulate = Color.White;
                    _menu.BeakerContentBox.EjectButton.Disabled = false;
                    _menu.ChamberContentBox.EjectButton.Disabled = false;
                    break;
            }
        }

        private void RefreshContentsDisplay(IList<ReagentQuantity>? reagents, IReadOnlyList<EntityUid> containedSolids, bool isBeakerAttached)
        {
            //Refresh chamber contents
            _chamberVisualContents.Clear();

            if (_menu == null)
            {
                return;
            }

            _menu.ChamberContentBox.BoxContents.Clear();
            foreach (var uid in containedSolids)
            {
                if (!_entityManager.TryGetEntity(uid, out var entity))
                {
                    return;
                }
                var texture = entity.GetComponent<SpriteComponent>().Icon?.Default;

                var solidItem = _menu.ChamberContentBox.BoxContents.AddItem(entity.Name, texture);
                var solidIndex = _menu.ChamberContentBox.BoxContents.IndexOf(solidItem);
                _chamberVisualContents.Add(solidIndex, uid);
            }

            //Refresh beaker contents
            _beakerVisualContents.Clear();
            _menu.BeakerContentBox.BoxContents.Clear();
            //if no beaker is attached use this guard to prevent hitting a null reference.
            if (!isBeakerAttached || reagents == null)
            {
                return;
            }

            //Looks like we have a beaker attached.
            if (reagents.Count <= 0)
            {
                _menu.BeakerContentBox.BoxContents.AddItem(Loc.GetString("grinder-menu-beaker-content-box-is-empty"));
            }
            else
            {
                for (var i = 0; i < reagents.Count; i++)
                {
                    var goodIndex = _prototypeManager.TryIndex(reagents[i].ReagentId, out ReagentPrototype? proto);
                    var reagentName = goodIndex ? Loc.GetString($"{reagents[i].Quantity} {proto!.Name}") : Loc.GetString("???");
                    var reagentAdded = _menu.BeakerContentBox.BoxContents.AddItem(reagentName);
                    var reagentIndex = _menu.BeakerContentBox.BoxContents.IndexOf(reagentAdded);
                    _beakerVisualContents.Add(reagentIndex, reagents[i]);
                }
            }
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

            //We'll need 4 buttons, grind, juice, eject beaker, eject the chamber contents.
            //The other 2 are referenced in the Open function.
            public Button GrindButton { get; }
            public Button JuiceButton { get; }

            public LabelledContentBox ChamberContentBox { get; }
            public LabelledContentBox BeakerContentBox { get; }

            public sealed class LabelledContentBox : VBoxContainer
            {
                public string? LabelText { get; set; }

                public ItemList BoxContents { get; set; }

                public Button EjectButton { get; set; }

                private Label _label;

                public LabelledContentBox(string labelText, string buttonText)
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
                        VerticalExpand = true,
                        HorizontalExpand = true,
                        SelectMode = ItemList.ItemListSelectMode.Button,
                        SizeFlagsStretchRatio = 2,
                        MinSize = (100, 128)
                    };
                    AddChild(BoxContents);
                }
            }

            public GrinderMenu(ReagentGrinderBoundUserInterface owner)
            {
                SetSize = MinSize = (512, 256);
                Owner = owner;
                Title = Loc.GetString("grinder-menu-title");

                var hSplit = new HBoxContainer();

                var vBoxGrindJuiceButtonPanel = new VBoxContainer
                {
                    VerticalAlignment = VAlignment.Center
                };

                GrindButton = new Button
                {
                    Text = Loc.GetString("grinder-menu-grind-button"),
                    TextAlign = Label.AlignMode.Center,
                    MinSize = (64, 64)
                };

                JuiceButton = new Button
                {
                    Text = Loc.GetString("grinder-menu-juice-button"),
                    TextAlign = Label.AlignMode.Center,
                    MinSize = (64, 64)
                };

                vBoxGrindJuiceButtonPanel.AddChild(GrindButton);
                //inner button padding
                vBoxGrindJuiceButtonPanel.AddChild(new Control
                {
                    MinSize = (0, 16),
                });
                vBoxGrindJuiceButtonPanel.AddChild(JuiceButton);

                ChamberContentBox = new LabelledContentBox(Loc.GetString("grinder-menu-chamber-content-box-label"), Loc.GetString("grinder-menu-chamber-content-box-button"))
                {
                    //Modulate = Color.Red,
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = 2,

                };

                BeakerContentBox = new LabelledContentBox(Loc.GetString("grinder-menu-beaker-content-box-label"), Loc.GetString("grinder-menu-beaker-content-box-button"))
                {
                    //Modulate = Color.Blue,
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = 2,
                };

                hSplit.AddChild(vBoxGrindJuiceButtonPanel);

                //Padding between the g/j buttons panel and the itemlist boxes panel.
                hSplit.AddChild(new Control
                {
                    MinSize = (16, 0),
                });
                hSplit.AddChild(ChamberContentBox);

                //Padding between the two itemlists.
                hSplit.AddChild(new Control
                {
                    MinSize = (8, 0),
                });
                hSplit.AddChild(BeakerContentBox);
                Contents.AddChild(hSplit);
            }
        }
    }
}
