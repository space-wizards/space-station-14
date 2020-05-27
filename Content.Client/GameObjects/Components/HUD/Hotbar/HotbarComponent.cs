using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    [RegisterComponent]
    public class HotbarComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IHotbarManager _hotbarManager;
#pragma warning restore 649

        public override string Name => "Hotbar";

        private HotbarGui _hotbarGui;
        private ActionMenu _actionMenu;
        private HotbarAction _menuActionSelected;

        private List<HotbarAction> _actions;
        private List<HotbarAction> _hotbar;

        public override void Initialize()
        {
            base.Initialize();

            _actions = new List<HotbarAction>();
            _hotbar = new List<HotbarAction>();

            for (int i = 0; i < 10; i++)
            {
                _hotbar.Add(null);
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _hotbarGui?.Dispose();
            _actionMenu?.Dispose();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg msg:
                {
                    if (_hotbarGui == null)
                    {
                        _hotbarGui = new HotbarGui();
                        _hotbarGui.OnToggled += SlotPressed;

                        _actionMenu = new ActionMenu();
                        _actionMenu.OnPressed += OnActionMenuItemSelected;
                        _actionMenu.OnClose += DeselectActionMenuItem;

                    }
                    else
                    {
                        _hotbarGui.Parent?.RemoveChild(_hotbarGui);
                    }

                    _gameHud.HotbarContainer.AddChild(_hotbarGui);
                    break;
                }
                case PlayerDetachedMsg _:
                {
                    _hotbarGui?.Parent?.RemoveChild(_hotbarGui);
                    break;
                }
            }
        }

        public void OpenActionMenu()
        {
            FetchActions();
            _actionMenu?.Open();
        }

        public void AddAction(HotbarAction action)
        {
            _actions.Add(action);
            _actionMenu?.Populate(_actions);
        }

        public void RemoveAction(HotbarAction action)
        {
            _actions.Remove(action);
            _actionMenu?.Populate(_actions);
        }

        public void TriggerAction(int index, PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            _hotbar[index]?.Activate(args);
        }

        private void FetchActions()
        {
            _actions.Clear();
            SendMessage(new GetActionsMessage(this));
        }

        private void OnActionMenuItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            _menuActionSelected = _actions[args.ItemIndex];
        }

        private void DeselectActionMenuItem()
        {
            _actionMenu?.ItemList.ClearSelected();
            _menuActionSelected = null;
        }

        public bool TryGetSlot(HotbarAction action, out int slot)
        {
            slot = _hotbar.IndexOf(action);
            if (slot == -1)
            {
                return false;
            }
            return true;
        }

        private void SetSlot(int slot, HotbarAction action)
        {
            if (slot < 0 || _hotbar.Count - 1 < slot)
            {
                return;
            }

            if (_hotbar[slot] != null)
            {
                _hotbarManager.UnbindUse(action);
            }

            _hotbar[slot] = action;
            _hotbarGui?.SetSlot(slot, action.Texture);
        }

        private void SlotPressed(BaseButton.ButtonToggledEventArgs args, int slot)
        {
            if (_menuActionSelected != null)
            {
                SetSlot(slot, _menuActionSelected);
                DeselectActionMenuItem();
                _hotbarGui?.UnpressSlot(slot);
            }
            else
            {
                if (_hotbar[slot] != null)
                {
                    _hotbar[slot]?.Toggle(args.Pressed);
                }
                else
                {
                    _hotbarGui?.UnpressSlot(slot);
                }
            }
        }

        public void UnpressHotbarAction(HotbarAction action)
        {
            var slot = _hotbar.IndexOf(action);
            if (slot == -1)
            {
                return;
            }

            _hotbarGui?.UnpressSlot(slot);
        }
    }
}
