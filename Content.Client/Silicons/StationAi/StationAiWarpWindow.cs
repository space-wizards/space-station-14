using System;
using System.Linq;
using System.Numerics;
using Content.Shared.Silicons.StationAi;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiWarpWindow : DefaultWindow
{
    public event Action<StationAiWarpTarget>? TargetSelected;

    private readonly LineEdit _searchBar;
    private readonly BoxContainer _listContainer;

    private List<StationAiWarpTarget> _targets = new();
    private string _searchText = string.Empty;
    private bool _isLoading;

    public StationAiWarpWindow()
    {
        Title = Loc.GetString("station-ai-warp-window-title");
        MinSize = new Vector2(380f, 420f);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 6,
        };

        _searchBar = new LineEdit
        {
            PlaceHolder = Loc.GetString("station-ai-warp-search-placeholder"),
        };
        _searchBar.OnTextChanged += OnSearchTextChanged;
        root.AddChild(_searchBar);

        _listContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 2,
        };

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };
        scroll.AddChild(_listContainer);
        root.AddChild(scroll);

        Contents.AddChild(root);

        PopulateList();
    }

    public void SetLoading(bool loading)
    {
        _isLoading = loading;
        if (loading)
            _targets.Clear();

        PopulateList();
    }

    public void SetTargets(IEnumerable<StationAiWarpTarget> targets)
    {
        var nameComparer = Comparer<string>.Create((x, y) => string.Compare(x, y, StringComparison.CurrentCultureIgnoreCase));

        _targets = targets
            .OrderBy(t => t.Type)
            .ThenBy(t => t.DisplayName, nameComparer)
            .ToList();
        _isLoading = false;
        PopulateList();
    }

    private void PopulateList()
    {
        _listContainer.DisposeAllChildren();

        if (_isLoading)
        {
            _listContainer.AddChild(new Label
            {
                Text = Loc.GetString("station-ai-warp-loading"),
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 8),
            });
            return;
        }

        var filtered = string.IsNullOrWhiteSpace(_searchText)
            ? _targets
            : _targets.Where(t => t.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filtered.Count == 0)
        {
            _listContainer.AddChild(new Label
            {
                Text = Loc.GetString("station-ai-warp-no-results"),
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 8),
            });
            return;
        }

        StationAiWarpTargetType? currentSection = null;

        foreach (var target in filtered)
        {
            if (currentSection != target.Type)
            {
                currentSection = target.Type;
                var headerText = currentSection == StationAiWarpTargetType.Crew
                    ? Loc.GetString("station-ai-warp-section-crew")
                    : Loc.GetString("station-ai-warp-section-locations");

                _listContainer.AddChild(new Label
                {
                    Text = headerText,
                    Margin = new Thickness(0, 6, 0, 2),
                    Modulate = Color.LightSkyBlue,
                });
            }

            var capturedTarget = target;
            var button = new Button
            {
                Text = capturedTarget.DisplayName,
                HorizontalAlignment = HAlignment.Stretch,
                ClipText = true,
            };

            button.OnPressed += _ => TargetSelected?.Invoke(capturedTarget);
            _listContainer.AddChild(button);
        }
    }

    private void OnSearchTextChanged(LineEdit.LineEditEventArgs args)
    {
        _searchText = args.Text;
        PopulateList();
    }
}
