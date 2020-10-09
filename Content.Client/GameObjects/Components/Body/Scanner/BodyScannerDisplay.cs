using System.Collections.Generic;
using Content.Shared.Body.Scanner;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.ItemList;

namespace Content.Client.GameObjects.Components.Body.Scanner
{
    public sealed class BodyScannerDisplay : SS14Window
    {
        private BodyScannerTemplateData _template;

        private Dictionary<string, BodyScannerBodyPartData> _parts;

        private List<string> _slots;

        private BodyScannerBodyPartData _currentBodyPart;

        public BodyScannerDisplay(BodyScannerBoundUserInterface owner)
        {
            IoCManager.InjectDependencies(this);
            Owner = owner;
            Title = Loc.GetString("Body Scanner");

            var hSplit = new HBoxContainer
            {
                Children =
                {
                    // Left half
                    new ScrollContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            (BodyPartList = new ItemList())
                        }
                    },
                    // Right half
                    new VBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            // Top half of the right half
                            new VBoxContainer
                            {
                                SizeFlagsVertical = SizeFlags.FillExpand,
                                Children =
                                {
                                    (BodyPartLabel = new Label()),
                                    new HBoxContainer
                                    {
                                        Children =
                                        {
                                            new Label
                                            {
                                                Text = "Health: "
                                            },
                                            (BodyPartHealth = new Label())
                                        }
                                    },
                                    new ScrollContainer
                                    {
                                        SizeFlagsVertical = SizeFlags.FillExpand,
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
                                SizeFlagsVertical = SizeFlags.FillExpand
                            })
                        }
                    }
                }
            };

            Contents.AddChild(hSplit);

            BodyPartList.OnItemSelected += BodyPartOnItemSelected;
            MechanismList.OnItemSelected += MechanismOnItemSelected;
        }

        public BodyScannerBoundUserInterface Owner { get; }

        protected override Vector2? CustomSize => (800, 600);

        private ItemList BodyPartList { get; }

        private Label BodyPartLabel { get; }

        private Label BodyPartHealth { get; }

        private ItemList MechanismList { get; }

        private RichTextLabel MechanismInfoLabel { get; }

        public void UpdateDisplay(BodyScannerTemplateData template, Dictionary<string, BodyScannerBodyPartData> parts)
        {
            _template = template;
            _parts = parts;
            _slots = new List<string>();
            BodyPartList.Clear();

            foreach (var slotName in _parts.Keys)
            {
                // We have to do this since ItemLists only return the index of what item is
                // selected and dictionaries don't allow you to explicitly grab things by index.
                // So we put the contents of the dictionary into a list so
                // that we can grab the list by index. I don't know either.
                _slots.Add(slotName);

                BodyPartList.AddItem(Loc.GetString(slotName));
            }
        }

        public void BodyPartOnItemSelected(ItemListSelectedEventArgs args)
        {
            if (_parts.TryGetValue(_slots[args.ItemIndex], out _currentBodyPart)) {
                UpdateBodyPartBox(_currentBodyPart, _slots[args.ItemIndex]);
            }
        }

        private void UpdateBodyPartBox(BodyScannerBodyPartData part, string slotName)
        {
            BodyPartLabel.Text = $"{Loc.GetString(slotName)}: {Loc.GetString(part.Name)}";
            BodyPartHealth.Text = $"{part.CurrentDurability}/{part.MaxDurability}";

            MechanismList.Clear();
            foreach (var mechanism in part.Mechanisms) {
                MechanismList.AddItem(mechanism.Name);
            }
        }

        public void MechanismOnItemSelected(ItemListSelectedEventArgs args)
        {
            UpdateMechanismBox(_currentBodyPart.Mechanisms[args.ItemIndex]);
        }

        private void UpdateMechanismBox(BodyScannerMechanismData mechanism)
        {
            // TODO: Improve UI
            if (mechanism == null)
            {
                MechanismInfoLabel.SetMessage("");
                return;
            }

            var message =
                Loc.GetString(
                    $"{mechanism.Name}\nHealth: {mechanism.CurrentDurability}/{mechanism.MaxDurability}\n{mechanism.Description}");

            MechanismInfoLabel.SetMessage(message);
        }
    }
}
