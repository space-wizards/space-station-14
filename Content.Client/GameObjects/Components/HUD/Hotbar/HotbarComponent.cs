using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    [RegisterComponent]
    public class HotbarComponent : Component
    {
        public override string Name => "Hotbar";

        public HotbarGui HotbarGui;
        public AbilityMenu AbilityMenu;
        public List<Ability> Abilities;
        public List<Ability> Hotbar;

        public override void Initialize()
        {
            base.Initialize();

            AbilityMenu = new AbilityMenu();
            HotbarGui = new HotbarGui();
            Abilities = new List<Ability>();
            Hotbar = new List<Ability>();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg msg:
                {
                    SendMessage(new GetAbilitiesMessage(this));
                    break;
                }
            }
        }

        public void AddAbility(Ability ability)
        {
            Abilities.Add(ability);
            if (Hotbar.Count < 10)
            {
                Hotbar.Add(ability);
            }
        }

        public void SetSlot(int slot, Ability ability)
        {
        }
    }
}
