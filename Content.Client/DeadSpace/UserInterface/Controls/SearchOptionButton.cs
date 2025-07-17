using Content.Shared.Humanoid.Markings;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.DeadSpace.UserInterface.Controls;

[Virtual]
public class SearchOptionButton : HeadedOptionButton
{
    private LineEdit _searchBar;

    /// <summary>
    ///     Search bar placeholder.
    /// </summary>
    [ViewVariables]
    public string? PlaceHolder { get => _searchBar.PlaceHolder; set => _searchBar.PlaceHolder = value; }

    public SearchOptionButton()
    {
        _searchBar = new LineEdit();
        _searchBar.OnTextChanged += OnSearchBarTextChanged;

        ScrollHeading.AddChild(_searchBar);
    }

    public void ResetSearch()
    {
        _searchBar.Text = "";
        FilterItems();
    }

    protected void FilterItems()
    {
        var query = _searchBar.Text.Trim().ToLowerInvariant();

        foreach (ButtonData data in _buttonData)
        {
            var buttonLabel = data.Text.ToLowerInvariant();

            if (!buttonLabel.Contains(query))
            {
                data.Button.Visible = false;
            }
            else
            {
                data.Button.Visible = true;
            }
        }
    }

    protected void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
    {
        FilterItems();
    }
}
