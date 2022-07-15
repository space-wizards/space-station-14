using Content.Client.Cuffs.Components;
using Content.Client.Examine;
using Content.Client.Hands;
using Content.Client.HUD;
using Content.Client.Items.UI;
using Content.Client.Resources;
using Content.Client.Strip;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Strip.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed class StrippableBoundUserInterface : BoundUserInterface
    {
        private int ButtonSize => ClientInventorySystem.ButtonSize;
        private const int ButtonSeparation = 4;

        private Texture BlockedTexture => _resourceCache.GetTexture(HandsGui.BlockedTexturePath);

        private IGameHud _hud = default!;
        private IPrototypeManager _protoMan = default!;
        private IEntityManager _entMan = default!;
        private IResourceCache _resourceCache = default!;
        private ExamineSystem _examine = default!;
        private InventorySystem _inv = default!;

        [ViewVariables]
        private StrippingMenu? _strippingMenu;

        public const string HiddenPocketEntityId = "StrippingHiddenEntity";
        private EntityUid _virtualHiddenEntity;

        public StrippableBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _entMan = IoCManager.Resolve<IEntityManager>();
            _hud = IoCManager.Resolve<IGameHud>();
            _protoMan = IoCManager.Resolve<IPrototypeManager>();
            _resourceCache = IoCManager.Resolve<IResourceCache>();
            _examine = _entMan.EntitySysManager.GetEntitySystem<ExamineSystem>();
            _inv = _entMan.EntitySysManager.GetEntitySystem<InventorySystem>();
            _strippingMenu = new StrippingMenu($"{Loc.GetString("strippable-bound-user-interface-stripping-menu-title", ("ownerName", Identity.Name(Owner.Owner, _entMan)))}");
            _virtualHiddenEntity = _entMan.SpawnEntity(HiddenPocketEntityId, MapCoordinates.Nullspace);

            _strippingMenu.OnClose += Close;
            UpdateMenu();
            _strippingMenu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _entMan.DeleteEntity(_virtualHiddenEntity);

            if (!disposing)
                return;

            _strippingMenu?.Dispose();
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

            // TODO fix layout container measuring (its broken atm).
            // _strippingMenu.InvalidateMeasure();
            // _strippingMenu.Contents.Measure(Vector2.Infinity);

            // TODO allow windows to resize based on content's desired size

            // for now: shit-code
            // this breaks for drones (too many hands, lots of empty vertical space), and looks shit for monkeys and the like.
            // but the window is realizable, so eh.
            _strippingMenu.SetSize = (220, 529);
        }

        private void AddHandButton(Hand hand)
        {
            var buttonTextureName = hand.Location switch
            {
                HandLocation.Right => "hand_r.png",
                _ => "hand_l.png"
            };

            var button = new HandButton(ClientInventorySystem.ButtonSize,
                buttonTextureName,
                ClientInventorySystem.StorageTexture,
                _hud,
                BlockedTexture,
                hand.Location);

            button.OnPressed += (ev) =>
            {
                // TODO: allow other interactions? Verbs?
                // But they should probably generator a pop-up and/or have a delay.
                if (ev.Function == EngineKeyFunctions.Use)
                    SendMessage(new StrippingHandButtonPressed(hand.Name));
                else if (ev.Function == ContentKeyFunctions.ExamineEntity && hand.HeldEntity != null)
                    _examine.DoExamine(hand.HeldEntity.Value);
            };

            if (_entMan.TryGetComponent(hand.HeldEntity, out HandVirtualItemComponent? virt))
            {
                button.Blocked.Visible = true;
                if (_entMan.TryGetComponent(Owner.Owner, out CuffableComponent? cuff) && cuff.Container.Contains(virt.BlockingEntity))
                {
                    button.Blocked.OnKeyBindDown += (ev) =>
                    {
                        if (ev.Function == EngineKeyFunctions.Use)
                            SendMessage(new StrippingHandcuffButtonPressed(virt.BlockingEntity));
                    };
                }
            }

            UpdateEntityIcon(button, hand.HeldEntity);
            _strippingMenu!.HandsContainer.AddChild(button);
        }

        private void AddInventoryButton(string slotId, InventoryTemplatePrototype template, InventoryComponent inv)
        {
            SlotDefinition? slotDef = null;
            foreach (var def in template.Slots)
            {
                if (!def.Name.Equals(slotId)) continue;
                slotDef = def;
                break;
            }

            if (slotDef == null)
                return;

            _inv.TryGetSlotEntity(inv.Owner, slotId, out var entity, inv);

            // If this is a full pocket, obscure the real entity
            if (entity != null && slotDef.StripHidden)
                entity = _virtualHiddenEntity;

            var button = new ItemSlotButton(ButtonSize, $"{slotDef.TextureName}.png", ClientInventorySystem.StorageTexture, _hud);
            button.OnPressed += (ev) =>
            {
                // TODO: allow other interactions? Verbs?
                // But they should probably generator a pop-up and/or have a delay.
                if (ev.Function == EngineKeyFunctions.Use)
                    SendMessage(new StrippingInventoryButtonPressed(slotId));
                else if (ev.Function == ContentKeyFunctions.ExamineEntity && entity != null)
                    _examine.DoExamine(entity.Value);
            };

            _strippingMenu!.InventoryContainer.AddChild(button);

            UpdateEntityIcon(button, entity);

            LayoutContainer.SetPosition(button, slotDef.StrippingWindowPos * (ButtonSize + ButtonSeparation));
        }

        private void UpdateEntityIcon(ItemSlotButton button, EntityUid? entity)
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
