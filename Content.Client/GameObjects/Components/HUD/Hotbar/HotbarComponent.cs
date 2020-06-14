using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.HUD.Hotbar;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    [RegisterComponent]
    public class HotbarComponent : SharedHotbarComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IHotbarManager _hotbarManager;
#pragma warning restore 649

        private HotbarGui _hotbarGui;
        private ActionMenu _actionMenu;
        private HotbarAction _menuActionSelected;

        private List<HotbarAction> _actions;
        private List<List<HotbarAction>> _hotbars;
        private List<HotbarAction> _hotbar;
        private int _hotbarIndex;

        public override void Initialize()
        {
            base.Initialize();

            _actions = new List<HotbarAction>();
            _hotbars = new List<List<HotbarAction>>();
            for (var i = 0; i < 10; i++)
            {
                var hotbar = new List<HotbarAction>();
                for (var j = 0; j < 10; j++)
                {
                    hotbar.Add(null);
                }
                _hotbars.Add(hotbar);
            }

            _hotbar = _hotbars[0];

            _hotbarGui = new HotbarGui();
            _hotbarGui.OnSlotToggled += SlotPressed;
            _hotbarGui.SettingsButton.OnPressed += ToggleActionMenu;
            _hotbarGui.NextHotbarButton.OnPressed += NextHotbar;
            _hotbarGui.PreviousHotbarButton.OnPressed += PreviousHotbar;

            _actionMenu = new ActionMenu();
            _actionMenu.OnPressed += OnActionMenuItemSelected;
            _actionMenu.OnClose += DeselectActionMenuItem;
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
                    _gameHud.HotbarContainer.AddChild(_hotbarGui);
                    break;
                }
                case PlayerDetachedMsg _:
                {
                    _gameHud.HotbarContainer.RemoveChild(_hotbarGui);
                    break;
                }
            }
        }

        public void ToggleActionMenu(BaseButton.ButtonEventArgs args)
        {
            if (_actionMenu.IsOpen)
            {
                _actionMenu.Close();
            }
            else
            {
                OpenActionMenu();
            }
        }

        public void NextHotbar(BaseButton.ButtonEventArgs args)
        {
            if (_hotbarIndex >= _hotbars.Count - 1)
                return;
            _hotbarIndex += 1;
            _hotbar = _hotbars[_hotbarIndex];
            UpdateHotbarSlots();
        }

        public void PreviousHotbar(BaseButton.ButtonEventArgs args)
        {
            if (_hotbarIndex <= 0)
                return;
            _hotbarIndex -= 1;
            _hotbar = _hotbars[_hotbarIndex];
            UpdateHotbarSlots();
        }

        private void UpdateHotbarSlots()
        {
            _hotbarGui.LoadoutNumber.Text = (_hotbarIndex + 1).ToString();
            for (var i = 0; i < _hotbar.Count; i++)
            {
                if (_hotbar[i] == null)
                {
                    _hotbarGui.SetSlot(i, null, false);
                }
                else
                {
                    _hotbarGui.SetSlot(i, _hotbar[i].Texture, _hotbar[i].Active);
                }
            }
        }

        public void OpenActionMenu()
        {
            FetchActions();
            _actionMenu?.Open();
        }

        public void AddActionToMenu(HotbarAction action)
        {
            _actions.Add(action);
            _actionMenu?.Populate(_actions);
        }

        public void RemoveActionFromMenu(HotbarAction action)
        {
            _actions.Remove(action);
            _actionMenu?.Populate(_actions);
        }

        public int GetSlotOf(HotbarAction action)
        {
            return _hotbar.IndexOf(action);
        }

        public void TriggerAction(int index, PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            _hotbar[index]?.Activate(args);
        }

        private void FetchActions()
        {
            _actions.Clear();
            SendMessage(new GetActionsMessage(this));

            var actions = _hotbarManager.GetGlobalActions();
            if (actions == null)
            {
                return;
            }
            _actions.AddRange(actions.Values);
            _actionMenu?.Populate(_actions);
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
            if (slot < 0 || _hotbar.Count - 1 < slot || _hotbar[slot] == action)
            {
                return;
            }

            if (_hotbar.Contains(action))
            {
                var index = _hotbar.IndexOf(action);
                _hotbar[index] = null;
                _hotbarGui?.SetSlot(index, null, false);
            }

            if (_hotbar[slot] != null)
            {
                _hotbar[slot].RemovedFromHotbar();
            }

            _hotbar[slot] = action;
            _hotbarGui?.SetSlot(slot, action.Texture, action.Active);
        }

        private void SlotPressed(BaseButton.ButtonToggledEventArgs args, int slot)
        {
            if (_menuActionSelected != null)
            {
                if (_hotbar[slot] != null)
                {
                    _hotbar[slot].Active = false;
                }
                SetSlot(slot, _menuActionSelected);
                DeselectActionMenuItem();
            }
            else
            {
                if (_hotbar[slot] != null)
                {
                    _hotbar[slot]?.Toggle(args.Pressed);
                }
                else
                {
                    _hotbarGui?.SetSlotPressed(slot, false);
                }
            }
        }

        public void SetHotbarSlotPressed(int slot, bool pressed)
        {
            if (slot == -1)
            {
                return;
            }

            if (_hotbar[slot] != null)
            {
                _hotbar[slot].Active = pressed;
            }
            _hotbarGui?.SetSlotPressed(slot, pressed);
        }
    }
}
