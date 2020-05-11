using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    [RegisterComponent]
    public class HotbarComponent : Component
    {
        public override string Name => "Hotbar";

        public HotbarGui HotbarGui;
        public AbilityMenu AbilityMenu;
        public List<Ability> Abilities = new List<Ability>();
        public List<Ability> Hotbar = new List<Ability>();

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
