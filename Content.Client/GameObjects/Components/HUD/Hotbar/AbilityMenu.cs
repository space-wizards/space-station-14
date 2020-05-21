using System;
using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    public class AbilityMenu : SS14Window
    {
        public ItemList ItemList;

        public event Action<ItemList.ItemListSelectedEventArgs> OnPressed;

        public AbilityMenu()
        {
            Title = "Abilities";
            CustomMinimumSize = (300, 300);

            ItemList = new ItemList();
            Contents.AddChild(ItemList);

            ItemList.OnItemSelected += (args) => OnPressed?.Invoke(args);
        }

        public void Populate(List<Ability> abilities)
        {
            ItemList.Clear();

            foreach (var ability in abilities)
            {
                ItemList.AddItem(ability.Name, ability.Texture);
            }
        }
    }
}
