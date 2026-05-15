using System.Linq;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

public sealed class SearchListContainer : ListContainer
{
    private LineEdit? _searchBar;
    private List<ListData> _unfilteredData = new();

    /// <summary>
    /// The <see cref="LineEdit"/> that is used to filter the list data.
    /// </summary>
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

    /// <summary>
    /// Runs over the ListData to determine if it should pass the filter.
    /// </summary>
    public Func<string, ListData, bool>? DataFilterCondition = null;

    public override void PopulateList(IReadOnlyList<ListData> data)
    {
        _unfilteredData = data.ToList();
        FilterList();
    }

    private void FilterList(LineEdit.LineEditEventArgs obj)
    {
        FilterList();
    }

    private void FilterList()
    {
        var filterText = SearchBar?.Text;

        if (DataFilterCondition is null || string.IsNullOrEmpty(filterText))
        {
            base.PopulateList(_unfilteredData);
            return;
        }

        var filteredData = new List<ListData>();
        foreach (var data in _unfilteredData)
        {
            if (!DataFilterCondition(filterText, data))
                continue;

            filteredData.Add(data);
        }

        base.PopulateList(filteredData);
    }
}
