using System.Linq;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

public sealed class SearchListContainer : Control
{
    private LineEdit? _searchBar;

    public LineEdit? SearchBar
    {
        get => _searchBar;
        set
        {
            if (_searchBar is not null)
                _searchBar.OnTextChanged -= FilterList;

            _searchBar = value;

            if (_searchBar is null)
                return;

            _searchBar.OnTextChanged += FilterList;
        }
    }

    private void FilterList(LineEdit.LineEditEventArgs obj)
    {
        FilterList();
    }

    public Func<string, ListData, bool>? DataFilterCondition;
    public Action<ListData, ListContainerButton>? GenerateItem
    {
        get => _listContainer.GenerateItem;
        set => _listContainer.GenerateItem = value;
    }
    public Action<BaseButton.ButtonEventArgs, ListData>? ItemPressed
    {
        get => _listContainer.ItemPressed;
        set => _listContainer.ItemPressed = value;
    }
    public Action<GUIBoundKeyEventArgs, ListData>? ItemKeyBindDown
    {
        get => _listContainer.ItemKeyBindDown;
        set => _listContainer.ItemKeyBindDown = value;
    }

    private List<ListData> _data = new();
    private readonly ListContainer _listContainer = new();

    public SearchListContainer()
    {
        AddChild(_listContainer);
    }

    private void FilterList()
    {
        var filterText = SearchBar?.Text;

        if (DataFilterCondition is null || string.IsNullOrEmpty(filterText))
        {
            _listContainer.PopulateList(_data);
            return;
        }

        var filteredData = new List<ListData>();
        foreach (var data in _data)
        {
            if (!DataFilterCondition(filterText, data))
                continue;

            filteredData.Add(data);
        }

        _listContainer.PopulateList(filteredData);
    }

    public void PopulateList(IReadOnlyList<ListData> data)
    {
        _data = data.ToList();
        FilterList();
    }
}
