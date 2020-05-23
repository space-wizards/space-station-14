using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Shared.Input.PointerInputCmdHandler;

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
        private AbilityMenu _abilityMenu;
        private Ability _menuAbilitySelected;

        private List<Ability> _abilities;
        private List<Ability> _hotbar;

        public override void Initialize()
        {
            base.Initialize();

            _abilities = new List<Ability>();
            _hotbar = new List<Ability>();

            for (int i = 0; i < 10; i++)
            {
                _hotbar.Add(null);
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _hotbarGui?.Dispose();
            _abilityMenu?.Dispose();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg msg:
                {
                    FetchAbilities();
                    if (_hotbarGui == null)
                    {
                        _hotbarGui = new HotbarGui();
                        _hotbarGui.OnToggled += SlotPressed;

                        _abilityMenu = new AbilityMenu();
                        _abilityMenu.OnPressed += OnAbilityMenuItemSelected;
                        _abilityMenu.OnClose += DeselectAbilityMenuItem;

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

        public void OpenAbilityMenu()
        {
            FetchAbilities();
            _abilityMenu?.Open();
        }

        public void AddAbility(Ability ability)
        {
            _abilities.Add(ability);
            _abilityMenu?.Populate(_abilities);
        }

        public void RemoveAbility(Ability ability)
        {
            _abilities.Remove(ability);
            _abilityMenu?.Populate(_abilities);
        }

        public void TriggerAbility(int index, PointerInputCmdArgs args)
        {
            _hotbar[index]?.Activate(args);
        }

        private void FetchAbilities()
        {
            _abilities.Clear();
            SendMessage(new GetAbilitiesMessage(this));
        }

        private void OnAbilityMenuItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            _menuAbilitySelected = _abilities[args.ItemIndex];
        }

        private void DeselectAbilityMenuItem()
        {
            _abilityMenu?.ItemList.ClearSelected();
            _menuAbilitySelected = null;
        }

        public bool TryGetSlot(Ability ability, out int slot)
        {
            slot = _hotbar.IndexOf(ability);
            if (slot == -1)
            {
                return false;
            }
            return true;
        }

        private void SetSlot(int slot, Ability ability)
        {
            if (slot < 0 || _hotbar.Count - 1 < slot)
            {
                return;
            }

            if (_hotbar[slot] != null)
            {
                _hotbarManager.UnbindUse(ability);
            }

            _hotbar[slot] = ability;
            _hotbarGui?.SetSlot(slot, ability.Texture);
        }

        private void SlotPressed(ButtonToggledEventArgs args, int slot)
        {
            if (_menuAbilitySelected != null)
            {
                SetSlot(slot, _menuAbilitySelected);
                DeselectAbilityMenuItem();
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

        public void UnpressHotbarAbility(Ability ability)
        {
            var slot = _hotbar.IndexOf(ability);
            if (slot == -1)
            {
                return;
            }

            _hotbarGui?.UnpressSlot(slot);
        }
    }
}
