using System.Collections.Generic;
using System.Globalization;
using Content.Shared.Health.BodySystem.BodyScanner;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.ItemList;

namespace Content.Client.Health.BodySystem.BodyScanner
{
    public sealed class BodyScannerDisplay : SS14Window
    {
        #pragma warning disable 649
                [Dependency] private readonly ILocalizationManager _loc;
        #pragma warning restore 649

        public BodyScannerBoundUserInterface Owner { get; private set; }
        protected override Vector2? CustomSize => (800, 600);
        private ItemList BodyPartList { get; }
        private Label BodyPartLabel { get; }
        private Label BodyPartHealth { get; }
        private ItemList MechanismList { get; }
        private RichTextLabel MechanismInfoLabel { get; }


        private BodyScannerTemplateData _template;
        private Dictionary<string, BodyScannerBodyPartData> _parts;
        private List<string> _slots;
        private BodyScannerBodyPartData _currentBodyPart;


        public BodyScannerDisplay(BodyScannerBoundUserInterface owner)
        {
            IoCManager.InjectDependencies(this);
            Owner = owner;
            Title = _loc.GetString("Body Scanner");

            var hSplit = new HBoxContainer();
            Contents.AddChild(hSplit);

            //Left half
            var scrollBox = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };
            hSplit.AddChild(scrollBox);
            BodyPartList = new ItemList { };
            scrollBox.AddChild(BodyPartList);
            BodyPartList.OnItemSelected += BodyPartOnItemSelected;

            //Right half
            var vSplit = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };
            hSplit.AddChild(vSplit);

            //Top half of the right half
            var limbBox = new VBoxContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            vSplit.AddChild(limbBox);
            BodyPartLabel = new Label();
            limbBox.AddChild(BodyPartLabel);
            var limbHealthHBox = new HBoxContainer();
            limbBox.AddChild(limbHealthHBox);
            var healthLabel = new Label
            {
                Text = "Health: "
            };
            limbHealthHBox.AddChild(healthLabel);
            BodyPartHealth = new Label();
            limbHealthHBox.AddChild(BodyPartHealth);
            var limbScroll = new ScrollContainer
            {
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            limbBox.AddChild(limbScroll);
            MechanismList = new ItemList();
            limbScroll.AddChild(MechanismList);
            MechanismList.OnItemSelected += MechanismOnItemSelected;

            //Bottom half of the right half
            MechanismInfoLabel = new RichTextLabel
            {
                SizeFlagsVertical = SizeFlags.FillExpand
            };
            vSplit.AddChild(MechanismInfoLabel);
        }


        public void UpdateDisplay(BodyScannerTemplateData template, Dictionary<string, BodyScannerBodyPartData> parts)
        {
            _template = template;
            _parts = parts;
            _slots = new List<string>();
            BodyPartList.Clear();
            foreach (var (key, value) in _parts)
            {
                _slots.Add(key);    //We have to do this since ItemLists only return the index of what item is selected and dictionaries don't allow you to explicitly grab things by index.
                                    //So we put the contents of the dictionary into a list so that we can grab the list by index. I don't know either.
                BodyPartList.AddItem(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(key));
            }
        }


        public void BodyPartOnItemSelected(ItemListSelectedEventArgs args)
        {
            if(_parts.TryGetValue(_slots[args.ItemIndex], out _currentBodyPart)) {
                UpdateBodyPartBox(_currentBodyPart, _slots[args.ItemIndex]);
            }
        }
        private void UpdateBodyPartBox(BodyScannerBodyPartData part, string slotName)
        {
            BodyPartLabel.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(slotName) + ": " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(part.Name);
            BodyPartHealth.Text = part.CurrentDurability + "/" + part.MaxDurability;

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
            //TODO: Make UI look less shit and clean up whatever the fuck this is lmao
            if (mechanism != null)
            {
                string message = "";
                message += mechanism.Name;
                message += "\nHealth: ";
                message += mechanism.CurrentDurability;
                message += "/";
                message += mechanism.MaxDurability;
                message += "\n";
                message += mechanism.Description;
                MechanismInfoLabel.SetMessage(message);
            }
            else
            {
                MechanismInfoLabel.SetMessage("");
            }
        }


    }
}
