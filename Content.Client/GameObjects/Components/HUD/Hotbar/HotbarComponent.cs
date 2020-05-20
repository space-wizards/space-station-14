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

        public List<Ability> _abilities;
        public List<Ability> Hotbar;

        public override void Initialize()
        {
            base.Initialize();

            _hotbarGui = new HotbarGui();
            _abilityMenu = new AbilityMenu();
            _abilities = new List<Ability>();
            Hotbar = new List<Ability>();

            _hotbarGui.OnPressed = SlotPressed;

            for (int i = 0; i < 10; i++)
            {
                Hotbar.Add(new Ability(null, null, null, null, null));
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg msg:
                {
                    GetAbilities();
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

        public void GetAbilities()
        {
            _abilities.Clear();
            SendMessage(new GetAbilitiesMessage(this));
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

        public void OnAbilityMenuItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            _menuAbilitySelected = _abilities[args.ItemIndex];
        }

        public void SetSlot(int slot, Ability ability)
        {
            if (slot < 0 || Hotbar.Count - 1 < slot)
            {
                return;
            }
            Hotbar[slot] = ability;
            _hotbarGui.SetSlot(slot, ability.Texture);
        }

        public void OpenAbilityMenu()
        {
            GetAbilities();
            _abilityMenu.OpenCentered();
        }

        private void SlotPressed(ButtonEventArgs args, int slot)
        {
            if (_menuAbilitySelected != null)
            {
                SetSlot(slot, _menuAbilitySelected);
            }
            else
            {
                Hotbar[slot].Select();
            }
        }
    }
}
