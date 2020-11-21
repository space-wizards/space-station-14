using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface
{
    /// <summary>
    /// Action selection menu, allows filtering and searching over all possible
    /// actions and populating those actions into the hotbar.
    /// </summary>
    public class ActionMenu : SS14Window
    {
        private ItemList _itemList;
        // parallel list of actions currently selectable in itemList
        private ActionPrototype[] _actionList;
        private ActionManager _actionManager;

        private event Action<ActionMenuItemSelectedEventArgs> _onItemSelected;

        /// <param name="onItemSelected">invoked when an action item
        /// in the list is clicked</param>
        public ActionMenu(Action<ActionMenuItemSelectedEventArgs> onItemSelected)
        {
            _onItemSelected = onItemSelected;
            _actionManager = IoCManager.Resolve<ActionManager>();
            Title = "Actions";
            CustomMinimumSize = (300, 300);

            _itemList = new ItemList();
            Contents.AddChild(_itemList);

            _itemList.OnItemSelected += OnItemSelected;
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            _onItemSelected?.Invoke(new ActionMenuItemSelectedEventArgs(args, _actionList[args.ItemIndex]));
        }

        protected override void Opened()
        {
            base.Opened();
            PopulateActions(_actionManager.EnumerateActions());
        }

        private void PopulateActions(IEnumerable<ActionPrototype> actions)
        {
            _itemList.Clear();

            _actionList = actions.ToArray();
            foreach (var action in _actionList)
            {
                _itemList.AddItem(action.Name.ToString(), action.Icon.Frame0());
            }
        }

        public override void Close()
        {
            base.Close();
            _itemList.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _itemList.OnItemSelected -= OnItemSelected;
        }

    }

    public class ActionMenuItemSelectedEventArgs : ItemList.ItemListSelectedEventArgs
    {
        public readonly ActionPrototype Action;

        public ActionMenuItemSelectedEventArgs(ItemList.ItemListSelectedEventArgs listArgs,
            ActionPrototype action) : base(listArgs.ItemIndex, listArgs.ItemList)
        {
            Action = action;
        }
    }
}
