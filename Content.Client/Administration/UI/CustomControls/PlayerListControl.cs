using System.Linq;
using Content.Shared.Administration;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls
{
    public sealed class PlayerListControl : FilteredItemList
    {
        private readonly AdminSystem _adminSystem;
        public event Action<PlayerInfo?>? OnSelectionChanged;

        public Action<PlayerInfo, ItemList.Item>? DecoratePlayer;
        public Comparison<PlayerInfo>? Comparison;

        public PlayerListControl()
        {
            _adminSystem = EntitySystem.Get<AdminSystem>();
            // Fill the Option data
            PopulateList();
            ItemList.OnItemSelected += PlayerItemListOnOnItemSelected;
            ItemList.OnItemDeselected += PlayerItemListOnOnItemDeselected;
            _adminSystem.PlayerListChanged += PopulateList;
        }

        private void PlayerItemListOnOnItemSelected(ItemList.ItemListSelectedEventArgs obj)
        {
            var selectedPlayer = (PlayerInfo) obj.ItemList[obj.ItemIndex].Metadata!;
            OnSelectionChanged?.Invoke(selectedPlayer);
        }

        private void PlayerItemListOnOnItemDeselected(ItemList.ItemListDeselectedEventArgs obj)
        {
            OnSelectionChanged?.Invoke(null);
        }

        public void RefreshDecorators()
        {
            foreach (var item in ItemList)
            {
                DecoratePlayer?.Invoke((PlayerInfo) item.Metadata!, item);
            }
            ReapplyFilter();
        }

        public void Sort()
        {
            if(Comparison != null)
                ItemList.Sort((a, b) => Comparison((PlayerInfo) a.Metadata!, (PlayerInfo) b.Metadata!));
        }

        public void Refresh() => PopulateList();

        private void PopulateList(IReadOnlyList<PlayerInfo> _ = null!)
        {
            ItemList.Clear();

            foreach (var info in _adminSystem.PlayerList)
            {
                var displayName = $"{info.CharacterName} ({info.Username})";

                var item = new ItemList.Item(ItemList)
                {
                    Metadata = info,
                    Text = displayName
                };
                DecoratePlayer?.Invoke(info, item);
                ItemList.Add(item);
            }

            Sort();
        }
    }
}
