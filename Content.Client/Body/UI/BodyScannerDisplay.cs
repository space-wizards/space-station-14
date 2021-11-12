using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.Controls.ItemList;

namespace Content.Client.Body.UI
{
    public sealed class BodyScannerDisplay : SS14Window
    {
        private IEntity? _currentEntity;
        private SharedBodyPartComponent? _currentBodyPart;

        private SharedBodyComponent? CurrentBody => _currentEntity?.GetComponentOrNull<SharedBodyComponent>();

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

        public void UpdateDisplay(IEntity entity)
        {
            _currentEntity = entity;
            BodyPartList.Clear();

            var body = CurrentBody;

            if (body == null)
            {
                return;
            }

            foreach (var (part, _) in body.Parts)
            {
                BodyPartList.AddItem(Loc.GetString(part.Name));
            }
        }

        public void BodyPartOnItemSelected(ItemListSelectedEventArgs args)
        {
            var body = CurrentBody;

            if (body == null)
            {
                return;
            }

            var slot = body.SlotAt(args.ItemIndex);
            _currentBodyPart = body.PartAt(args.ItemIndex).Key;

            if (slot.Part != null)
            {
                UpdateBodyPartBox(slot.Part, slot.Id);
            }
        }

        private void UpdateBodyPartBox(SharedBodyPartComponent part, string slotName)
        {
            BodyPartLabel.Text = $"{Loc.GetString(slotName)}: {Loc.GetString(part.Owner.Name)}";

            // TODO BODY Part damage
            if (part.Owner.TryGetComponent(out DamageableComponent? damageable))
            {
                BodyPartHealth.Text = Loc.GetString("body-scanner-display-body-part-damage-text",("damage", damageable.TotalDamage));
            }

            MechanismList.Clear();

            foreach (var mechanism in part.Mechanisms)
            {
                MechanismList.AddItem(mechanism.Name);
            }
        }

        // TODO BODY Guaranteed this is going to crash when a part's mechanisms change. This part is left as an exercise for the reader.
        public void MechanismOnItemSelected(ItemListSelectedEventArgs args)
        {
            UpdateMechanismBox(_currentBodyPart?.Mechanisms.ElementAt(args.ItemIndex));
        }

        private void UpdateMechanismBox(SharedMechanismComponent? mechanism)
        {
            // TODO BODY Improve UI
            if (mechanism == null)
            {
                MechanismInfoLabel.SetMessage("");
                return;
            }

            // TODO BODY Mechanism description
            var message =
                Loc.GetString(
                    $"{mechanism.Name}\nHealth: {mechanism.CurrentDurability}/{mechanism.MaxDurability}");

            MechanismInfoLabel.SetMessage(message);
        }
    }
}
