#nullable enable
using System.Linq;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.ItemList;

namespace Content.Client.GameObjects.Components.Body.Scanner
{
    public sealed class BodyScannerDisplay : SS14Window
    {
        private IEntity? _currentEntity;
        private IBodyPart? _currentBodyPart;

        private IBody? CurrentBody => _currentEntity?.GetBody();

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

        public void UpdateDisplay(IEntity entity)
        {
            _currentEntity = entity;
            BodyPartList.Clear();

            var body = CurrentBody;

            if (body == null)
            {
                return;
            }

            foreach (var slotName in body.Parts.Keys)
            {
                BodyPartList.AddItem(Loc.GetString(slotName));
            }
        }

        public void BodyPartOnItemSelected(ItemListSelectedEventArgs args)
        {
            var body = CurrentBody;

            if (body == null)
            {
                return;
            }

            var slot = body.SlotAt(args.ItemIndex).Key;
            _currentBodyPart = body.PartAt(args.ItemIndex).Value;

            if (body.Parts.TryGetValue(slot, out var part))
            {
                UpdateBodyPartBox(part, slot);
            }
        }

        private void UpdateBodyPartBox(IBodyPart part, string slotName)
        {
            BodyPartLabel.Text = $"{Loc.GetString(slotName)}: {Loc.GetString(part.Owner.Name)}";

            // TODO BODY Make dead not be the destroy threshold for a body part
            if (part.Owner.TryGetComponent(out IDamageableComponent? damageable) &&
                damageable.TryHealth(DamageState.Critical, out var health))
            {
                BodyPartHealth.Text = $"{health.current} / {health.max}";
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

        private void UpdateMechanismBox(IMechanism? mechanism)
        {
            // TODO BODY Improve UI
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
