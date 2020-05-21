using System;
using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
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

            _hotbarGui = new HotbarGui();
            _abilityMenu = new AbilityMenu();
            _abilities = new List<Ability>();
            _hotbar = new List<Ability>();

            _abilityMenu.OnPressed += OnAbilityMenuItemSelected;
            _abilityMenu.OnClose += DeselectAbilityMenuItem;
            _hotbarGui.OnToggled += SlotPressed;

            for (int i = 0; i < 10; i++)
            {
                _hotbar.Add(null);
            }
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
                    _hotbarGui.Parent?.RemoveChild(_hotbarGui);
                    break;
                }
            }
        }

        public void OpenAbilityMenu()
        {
            FetchAbilities();
            _abilityMenu.Open();
        }

        public void AddAbility(Ability ability)
        {
            _abilities.Add(ability);
            _abilityMenu.Populate(_abilities);
        }

        public void RemoveAbility(Ability ability)
        {
            _abilities.Remove(ability);
            _abilityMenu.Populate(_abilities);
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
            _abilityMenu.ItemList.ClearSelected();
            _menuAbilitySelected = null;
        }

        private void SetSlot(int slot, Ability ability)
        {
            if (slot < 0 || _hotbar.Count - 1 < slot)
            {
                return;
            }

            if (_hotbar[slot])
            _hotbar[slot] = ability;
            _hotbarGui.SetSlot(slot, ability.Texture);
        }

        private void SlotPressed(ButtonToggledEventArgs args, int slot)
        {
            if (_menuAbilitySelected != null)
            {
                SetSlot(slot, _menuAbilitySelected);
                DeselectAbilityMenuItem();
                _hotbarGui.UnpressSlot(slot);
            }
            else
            {
                _hotbar[slot].Select(args);
            }
        }

        public void UnpressHotbarSlot(Ability ability)
        {
            var index = _hotbar.IndexOf(ability);
            _hotbarGui.UnpressSlot(index);
        }
    }
}
