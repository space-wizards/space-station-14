using Content.Client.Cuffs.Components;
using Content.Client.Examine;
using Content.Client.Hands;
using Content.Client.Strip;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Hands.Controls;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Strip.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Content.Client.Inventory.ClientInventorySystem;
using static Robust.Client.UserInterface.Control;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed class StrippableBoundUserInterface : BoundUserInterface
    {
        private const int ButtonSeparation = 4;
        
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        private ExamineSystem _examine = default!;
        private InventorySystem _inv = default!;

        [ViewVariables]
        private StrippingMenu? _strippingMenu;

        public const string HiddenPocketEntityId = "StrippingHiddenEntity";
        private EntityUid _virtualHiddenEntity;

        public StrippableBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
            _examine = _entMan.EntitySysManager.GetEntitySystem<ExamineSystem>();
            _inv = _entMan.EntitySysManager.GetEntitySystem<InventorySystem>();
            var title = Loc.GetString("strippable-bound-user-interface-stripping-menu-title", ("ownerName", Identity.Name(Owner.Owner, _entMan)));
            _strippingMenu = new StrippingMenu(title, this);
            _strippingMenu.OnClose += Close;
            _virtualHiddenEntity = _entMan.SpawnEntity(HiddenPocketEntityId, MapCoordinates.Nullspace);
        }

        protected override void Open()
        {
            base.Open();
            _strippingMenu?.OpenCenteredLeft();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _entMan.DeleteEntity(_virtualHiddenEntity);

            if (!disposing)
                return;

            _strippingMenu?.Dispose();
        }

        public void DirtyMenu()
        {
            if (_strippingMenu != null)
                _strippingMenu.Dirty = true;
        }

        public void UpdateMenu()
        {
            if (_strippingMenu == null)
                return;

            _strippingMenu.ClearButtons();

            if (_entMan.TryGetComponent(Owner.Owner, out InventoryComponent? inv) && _protoMan.TryIndex<InventoryTemplatePrototype>(inv.TemplateId, out var template))
            {
                foreach (var slot in template.Slots)
                {
                    AddInventoryButton(slot.Name, template, inv);
                }
            }

            if (_entMan.TryGetComponent(Owner.Owner, out HandsComponent? handsComp))
            {
                // good ol hands shit code. there is a GuiHands comparer that does the same thing... but these are hands
                // and not gui hands... which are different... 
                foreach (var hand in handsComp.Hands.Values)
                {
                    if (hand.Location != HandLocation.Right)
                        continue;

                    AddHandButton(hand);
                }

                foreach (var hand in handsComp.Hands.Values)
                {
                    if (hand.Location != HandLocation.Middle)
                        continue;

                    AddHandButton(hand);
                }

                foreach (var hand in handsComp.Hands.Values)
                {
                    if (hand.Location != HandLocation.Left)
                        continue;

                    AddHandButton(hand);
                }
            }

            // snare-removal button. This is just the old button before the change to item slots. It is pretty out of place.
            if (_entMan.TryGetComponent(Owner.Owner, out EnsnareableComponent? snare) && snare.IsEnsnared)
            {
                var button = new Button()
                {
                    Text = Loc.GetString("strippable-bound-user-interface-stripping-menu-ensnare-button"),
                    StyleClasses = { StyleBase.ButtonOpenRight }
                };

                button.OnPressed += (_) => SendMessage(new StrippingEnsnareButtonPressed());

                _strippingMenu.SnareContainer.AddChild(button);
            }

            // TODO fix layout container measuring (its broken atm).
            // _strippingMenu.InvalidateMeasure();
            // _strippingMenu.Contents.Measure(Vector2.Infinity);

            // TODO allow windows to resize based on content's desired size

            // for now: shit-code
            // this breaks for drones (too many hands, lots of empty vertical space), and looks shit for monkeys and the like.
            // but the window is realizable, so eh.
            _strippingMenu.SetSize = (220, snare?.IsEnsnared == true ? 550 : 530);
        }

        private void AddHandButton(Hand hand)
        {
            var button = new HandButton(hand.Name, hand.Location);

            button.Pressed += SlotPressed;

            if (_entMan.TryGetComponent(hand.HeldEntity, out HandVirtualItemComponent? virt))
            {
                button.Blocked = true;
                if (_entMan.TryGetComponent(Owner.Owner, out CuffableComponent? cuff) && cuff.Container.Contains(virt.BlockingEntity))
                    button.BlockedRect.MouseFilter = MouseFilterMode.Ignore;
            }
            
            UpdateEntityIcon(button, hand.HeldEntity);
            _strippingMenu!.HandsContainer.AddChild(button);
        }

        private void SlotPressed(GUIBoundKeyEventArgs ev, SlotControl slot)
        {
            // TODO: allow other interactions? Verbs? But they should then generate a pop-up and/or have a delay so the
            // user that is being stripped can prevent the verbs from being exectuted.
            // So for now: only stripping & examining
            if (ev.Function == EngineKeyFunctions.Use)
            {
                SendMessage(new StrippingSlotButtonPressed(slot.SlotName, slot is HandButton));
            }
            else if (ev.Function == ContentKeyFunctions.ExamineEntity && slot.Entity != null)
            {
                _examine.DoExamine(slot.Entity.Value);
                return;
            }

            if (ev.Function != EngineKeyFunctions.Use)
                return;
        }

        private void AddInventoryButton(string slotId, InventoryTemplatePrototype template, InventoryComponent inv)
        {
            if (!_inv.TryGetSlotContainer(inv.Owner, slotId, out var container, out var slotDef, inv))
                return;

            var entity = container.ContainedEntity;

            // If this is a full pocket, obscure the real entity
            if (entity != null && slotDef.StripHidden)
                entity = _virtualHiddenEntity;

            var button = new SlotButton(new SlotData(slotDef, container));
            button.Pressed += SlotPressed;

            _strippingMenu!.InventoryContainer.AddChild(button);

            UpdateEntityIcon(button, entity);

            LayoutContainer.SetPosition(button, slotDef.StrippingWindowPos * (SlotControl.DefaultButtonSize + ButtonSeparation));
        }

        private void UpdateEntityIcon(SlotControl button, EntityUid? entity)
        {
            // Hovering, highlighting & storage are features of general hands & inv GUIs. This UI just re-uses these because I'm lazy.
            button.ClearHover();
            button.StorageButton.Visible = false;

            if (entity == null)
            {
                button.SpriteView.Sprite = null;
                return;
            }

            SpriteComponent? sprite;
            if (_entMan.TryGetComponent(entity, out HandVirtualItemComponent? virt))
                _entMan.TryGetComponent(virt.BlockingEntity, out sprite);
            else if (!_entMan.TryGetComponent(entity, out sprite))
                return;

            button.SpriteView.Sprite = sprite;
        }
    }
}
