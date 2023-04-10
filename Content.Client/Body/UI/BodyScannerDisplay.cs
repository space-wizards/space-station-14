using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.Controls.ItemList;

namespace Content.Client.Body.UI
{
    public sealed class BodyScannerDisplay : DefaultWindow
    {
        private EntityUid? _currentEntity;
        private BodyPartComponent? _currentBodyPart;
        private readonly Dictionary<int, BodyPartSlot> _bodyPartsList = new();

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

        public void UpdateDisplay(EntityUid entity)
        {
            _currentEntity = entity;
            BodyPartList.Clear();
            _bodyPartsList.Clear();

            var bodySystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedBodySystem>();
            var factory = IoCManager.Resolve<IComponentFactory>();
            var i = 0;
            foreach (var part in bodySystem.GetBodyChildren(_currentEntity))
            {
                _bodyPartsList[i++] = part.Component.ParentSlot!;
                BodyPartList.AddItem(Loc.GetString(factory.GetComponentName(part.Component.GetType())));
            }
        }

        public void BodyPartOnItemSelected(ItemListSelectedEventArgs args)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            _currentBodyPart = entMan.GetComponentOrNull<BodyPartComponent>(_bodyPartsList[args.ItemIndex].Child);

            if (_currentBodyPart is {ParentSlot.Id: var slotId} part)
            {
                UpdateBodyPartBox(part, slotId);
            }
        }

        private void UpdateBodyPartBox(BodyPartComponent part, string slotName)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            BodyPartLabel.Text =
                $"{Loc.GetString(slotName)}: {Loc.GetString(entMan.GetComponent<MetaDataComponent>(part.Owner).EntityName)}";

            // TODO BODY Part damage
            if (entMan.TryGetComponent(part.Owner, out DamageableComponent? damageable))
            {
                BodyPartHealth.Text = Loc.GetString("body-scanner-display-body-part-damage-text",
                    ("damage", damageable.TotalDamage));
            }

            MechanismList.Clear();

            var bodySystem = entMan.System<SharedBodySystem>();
            foreach (var organ in bodySystem.GetPartOrgans(part.Owner, part))
            {
                var organName = entMan.GetComponent<MetaDataComponent>(organ.Id).EntityName;
                MechanismList.AddItem(organName);
            }
        }

        // TODO BODY Guaranteed this is going to crash when a part's mechanisms change. This part is left as an exercise for the reader.
        public void MechanismOnItemSelected(ItemListSelectedEventArgs args)
        {
            if (_currentBodyPart == null)
            {
                UpdateMechanismBox(null);
                return;
            }

            var bodySystem = IoCManager.Resolve<IEntityManager>().System<SharedBodySystem>();
            var organ = bodySystem.GetPartOrgans(_currentBodyPart.Owner, _currentBodyPart).ElementAt(args.ItemIndex);
            UpdateMechanismBox(organ.Id);
        }

        private void UpdateMechanismBox(EntityUid? organ)
        {
            // TODO BODY Improve UI
            if (organ == null)
            {
                MechanismInfoLabel.SetMessage("");
                return;
            }

            // TODO BODY Mechanism description
            var entMan = IoCManager.Resolve<IEntityManager>();
            var organName = entMan.GetComponent<MetaDataComponent>(organ.Value).EntityName;
            var message = Loc.GetString($"{organName}");
            MechanismInfoLabel.SetMessage(message);
        }
    }
}
