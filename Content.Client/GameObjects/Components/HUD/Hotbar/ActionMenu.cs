using System;
using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    public class ActionMenu : SS14Window
    {
        public ItemList ItemList;

        public event Action<ItemList.ItemListSelectedEventArgs> OnPressed;

        public ActionMenu()
        {
            Title = "Actions";
            CustomMinimumSize = (300, 300);

            ItemList = new ItemList();
            Contents.AddChild(ItemList);

            ItemList.OnItemSelected += (args) => OnPressed?.Invoke(args);
        }

        public void Populate(List<HotbarAction> actions)
        {
            ItemList.Clear();

            foreach (var action in actions)
            {
                ItemList.AddItem(action.Name, action.Texture);
            }
        }
    }
}
