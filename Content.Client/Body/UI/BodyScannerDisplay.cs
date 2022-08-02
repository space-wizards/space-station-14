using System.Linq;
using Content.Shared.Body.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.Controls.ItemList;

namespace Content.Client.Body.UI
{
    public sealed class BodyScannerDisplay : DefaultWindow
    {
        private BodyScannerUIState? _currentState;
        private BodyPartUiState? _currentBodyPart;

        public BodyScannerDisplay(BodyScannerBoundUserInterface owner)
        {
            IoCManager.InjectDependencies(this);
            Owner = owner;
            Title = Loc.GetString("body-scanner-display-title");

            var hSplit = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    // Left half
                    new ScrollContainer
                    {
                        HorizontalExpand = true,
                        Children =
                        {
                            (BodyPartList = new ItemList())
                        }
                    },
                    // Right half
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        HorizontalExpand = true,
                        Children =
                        {
                            // Top half of the right half
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical,
                                VerticalExpand = true,
                                Children =
                                {
                                    (BodyPartLabel = new Label()),
                                    new BoxContainer
                                    {
                                        Orientation = LayoutOrientation.Horizontal,
                                        Children =
                                        {
                                            new Label
                                            {
                                                Text = $"{Loc.GetString("body-scanner-display-health-label")} "
                                            },
                                            (BodyPartHealth = new Label())
                                        }
                                    },
                                    new ScrollContainer
                                    {
                                        VerticalExpand = true,
                                        Children =
                                        {
                                            (MechanismList = new ItemList())
                                        }
                                    }
                                }
                            },
                            // Bottom half of the right half
                            (MechanismInfoLabel = new RichTextLabel
                            {
                                VerticalExpand = true
                            })
                        }
                    }
                }
            };

            Contents.AddChild(hSplit);

            BodyPartList.OnItemSelected += BodyPartOnItemSelected;
            MechanismList.OnItemSelected += MechanismOnItemSelected;
            MinSize = SetSize = (800, 600);
        }

        public BodyScannerBoundUserInterface Owner { get; }

        private ItemList BodyPartList { get; }

        private Label BodyPartLabel { get; }

        private Label BodyPartHealth { get; }

        private ItemList MechanismList { get; }

        private RichTextLabel MechanismInfoLabel { get; }

        public void UpdateDisplay(BodyScannerUIState state)
        {
            _currentState = state;
            BodyPartList.Clear();

            foreach (var (_, part) in state.BodyParts)
            {
                BodyPartList.AddItem(Loc.GetString(part.Name));
            }
        }

        public void BodyPartOnItemSelected(ItemListSelectedEventArgs args)
        {
            if (_currentState == null)
                return;

            var slotId = _currentState.BodyParts.Keys.ElementAt(args.ItemIndex);
            _currentBodyPart = _currentState.BodyParts[slotId];

            UpdateBodyPartBox(_currentBodyPart, slotId);
        }

        private void UpdateBodyPartBox(BodyPartUiState part, string slotName)
        {
            BodyPartLabel.Text = $"{Loc.GetString(slotName)}: {Loc.GetString(part.Name)}";

            // TODO BODY Part damage
            BodyPartHealth.Text = Loc.GetString("body-scanner-display-body-part-damage-text", ("damage", part.TotalDamage));

            MechanismList.Clear();

            foreach (var mechanism in part.Mechanisms)
            {
                MechanismList.AddItem(mechanism);
            }
        }

        public void MechanismOnItemSelected(ItemListSelectedEventArgs args)
        {
            if (_currentBodyPart == null)
                return;

            UpdateMechanismBox(_currentBodyPart.Mechanisms.ElementAt(args.ItemIndex));
        }

        private void UpdateMechanismBox(string mechanism)
        {
            // TODO BODY Improve UI
 
            // TODO BODY Mechanism description
            MechanismInfoLabel.SetMessage(Loc.GetString(mechanism));
        }
    }
}
