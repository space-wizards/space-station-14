using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls;

/// <summary>
///     A purchase row for tiered upgrades: an identity column (category tag, name, rank progress)
///     followed by one clickable cell per rank showing cost and effect.
///     Reusable by any "spend resource to buy ranks" menu; call <see cref="Refresh"/> whenever
///     the owned rank or the player's balance changes.
/// </summary>
public sealed class RankedUpgradeRow : PanelContainer
{
    /// <summary>Palette for the row. <see cref="Default"/> is a dark theme with gold/red accents.</summary>
    public sealed class RowStyle
    {
        public Color PanelBorder = Color.FromHex("#434863");
        public Color PanelBg = Color.FromHex("#3a3b46");
        public Color IdentityBg = Color.FromHex("#232430");
        public Color TagBg = Color.FromHex("#871517");
        public Color TagText = Color.FromHex("#f3c9a0");
        public Color Accent = Color.FromHex("#a88b5e");
        public Color OwnedBorder = Color.FromHex("#b62124");
        public Color OwnedBg = new(0.525f, 0.078f, 0.094f, 0.18f);
        public Color NextBg = new(0.659f, 0.545f, 0.369f, 0.08f);
        public Color LockedBorder = Color.FromHex("#2f334b");
        public Color LockedBg = Color.FromHex("#232430");
        public Color MutedText = Color.FromHex("#a9a9a9");
        public Color GoodText = Color.FromHex("#3c854a");
        public Color CostText = Color.FromHex("#ff6b6b");

        public static RowStyle Default => new();
    }

    /// <summary>Raised when the player clicks the next affordable rank.</summary>
    public event Action? OnPurchase;

    /// <summary>Suffix appended to cost numbers, e.g. a unit like "u".</summary>
    public string CurrencySuffix = string.Empty;

    /// <summary>Verb shown on the buy call-to-action, e.g. PURCHASE or EVOLVE.</summary>
    public string PurchaseVerb = Loc.GetString("ranked-upgrade-row-purchase");

    private readonly RowStyle _style;
    private readonly IReadOnlyList<int> _costs;
    private readonly Label _rankProgress;
    private readonly RankCell[] _cells;

    public RankedUpgradeRow(string name, string category, IReadOnlyList<string> effects, IReadOnlyList<int> costs, RowStyle? style = null)
    {
        _style = style ?? RowStyle.Default;
        _costs = costs;

        HorizontalExpand = true;
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = _style.PanelBg,
            BorderColor = _style.PanelBorder,
            BorderThickness = new Thickness(2),
        };

        var body = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
        AddChild(body);

        // ── Identity column (170px fixed) ──
        var identityPanel = new PanelContainer
        {
            MinWidth = 170,
            MaxWidth = 170,
            PanelOverride = new StyleBoxFlat { BackgroundColor = _style.IdentityBg },
        };
        var identityBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10, 8),
        };
        identityPanel.AddChild(identityBox);

        // Category tag pill
        var tagPanel = new PanelContainer
        {
            HorizontalAlignment = HAlignment.Left,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = _style.TagBg,
                BorderColor = _style.PanelBorder,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 6,
                ContentMarginRightOverride = 6,
                ContentMarginTopOverride = 1,
                ContentMarginBottomOverride = 1,
            },
        };
        tagPanel.AddChild(new Label
        {
            Text = category,
            FontColorOverride = _style.TagText,
            StyleClasses = { "LabelSubText" },
        });
        identityBox.AddChild(tagPanel);

        identityBox.AddChild(new Label
        {
            Text = name,
            FontColorOverride = _style.Accent,
            StyleClasses = { "LabelHeading" },
            Margin = new Thickness(0, 4, 0, 0),
        });

        _rankProgress = new Label
        {
            StyleClasses = { "LabelSubText" },
            Margin = new Thickness(0, 2, 0, 0),
        };
        identityBox.AddChild(_rankProgress);

        body.AddChild(identityPanel);
        body.AddChild(MakeSeparator());

        // ── One cell per rank ──
        _cells = new RankCell[costs.Count];
        var rankBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };

        for (var i = 0; i < costs.Count; i++)
        {
            if (i > 0)
                rankBox.AddChild(MakeSeparator());

            var effect = i < effects.Count ? effects[i] : string.Empty;
            _cells[i] = BuildRankCell(effect);
            rankBox.AddChild(_cells[i].Root);
        }

        body.AddChild(rankBox);
    }

    private RankCell BuildRankCell(string effect)
    {
        var root = new ContainerButton
        {
            HorizontalExpand = true,
            Disabled = true,
        };

        var inner = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(8),
            HorizontalExpand = true,
        };
        root.AddChild(inner);

        var headerRow = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalExpand = true };
        var rankLabel = new Label { StyleClasses = { "LabelSubText" } };
        var spacer = new Control { HorizontalExpand = true };
        var costLabel = new Label { StyleClasses = { "LabelSubText" } };
        headerRow.AddChild(rankLabel);
        headerRow.AddChild(spacer);
        headerRow.AddChild(costLabel);
        inner.AddChild(headerRow);

        inner.AddChild(new Label
        {
            Text = effect,
            StyleClasses = { "LabelSubText" },
            HorizontalExpand = true,
            ClipText = true,
            Margin = new Thickness(0, 4, 0, 0),
        });

        var ctaLabel = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            HorizontalExpand = true,
            ClipText = true,
            Margin = new Thickness(0, 4, 0, 0),
            Visible = false,
        };
        inner.AddChild(ctaLabel);

        root.OnPressed += _ => OnPurchase?.Invoke();

        return new RankCell(root, rankLabel, costLabel, ctaLabel);
    }

    /// <summary>Restyles all cells for the given owned rank and spendable balance.</summary>
    public void Refresh(int ownedRank, float balance)
    {
        var maxRank = _costs.Count;
        var maxed = ownedRank >= maxRank;

        _rankProgress.Text = maxed
            ? Loc.GetString("ranked-upgrade-row-progress-max", ("max", ToRoman(maxRank)))
            : Loc.GetString("ranked-upgrade-row-progress",
                ("rank", ownedRank == 0 ? "0" : ToRoman(ownedRank)),
                ("max", ToRoman(maxRank)));
        _rankProgress.FontColorOverride = maxed ? _style.GoodText : _style.MutedText;

        for (var i = 0; i < _cells.Length; i++)
        {
            var cell = _cells[i];
            var cost = _costs[i];
            var numeral = ToRoman(i + 1);

            if (i < ownedRank)
                ApplyOwned(cell, numeral);
            else if (i == ownedRank && balance >= cost)
                ApplyNext(cell, numeral, cost);
            else if (i == ownedRank)
                ApplyNextUnaffordable(cell, numeral, cost, cost - balance);
            else
                ApplyLocked(cell, numeral, cost);
        }
    }

    private void ApplyOwned(RankCell cell, string numeral)
    {
        cell.Root.StyleBoxOverride = CellBox(_style.OwnedBg, _style.OwnedBorder);
        cell.Root.Disabled = true;
        cell.RankLabel.Text = Loc.GetString("ranked-upgrade-row-rank-owned", ("rank", numeral));
        cell.RankLabel.FontColorOverride = _style.OwnedBorder;
        cell.CostLabel.Text = Loc.GetString("ranked-upgrade-row-paid");
        cell.CostLabel.FontColorOverride = _style.GoodText;
        cell.CtaLabel.Visible = false;
    }

    private void ApplyNext(RankCell cell, string numeral, int cost)
    {
        cell.Root.StyleBoxOverride = CellBox(_style.NextBg, _style.Accent);
        cell.Root.Disabled = false;
        cell.RankLabel.Text = Loc.GetString("ranked-upgrade-row-rank-next", ("rank", numeral));
        cell.RankLabel.FontColorOverride = _style.Accent;
        cell.CostLabel.Text = FormatCost(cost);
        cell.CostLabel.FontColorOverride = _style.CostText;
        cell.CtaLabel.Text = Loc.GetString("ranked-upgrade-row-cta", ("verb", PurchaseVerb), ("cost", FormatCost(cost)));
        cell.CtaLabel.FontColorOverride = _style.CostText;
        cell.CtaLabel.Visible = true;
    }

    private void ApplyNextUnaffordable(RankCell cell, string numeral, int cost, float shortfall)
    {
        cell.Root.StyleBoxOverride = CellBox(_style.NextBg, _style.Accent);
        cell.Root.Disabled = true;
        cell.RankLabel.Text = Loc.GetString("ranked-upgrade-row-rank-next", ("rank", numeral));
        cell.RankLabel.FontColorOverride = _style.Accent;
        cell.CostLabel.Text = FormatCost(cost);
        cell.CostLabel.FontColorOverride = _style.MutedText;
        cell.CtaLabel.Text = Loc.GetString("ranked-upgrade-row-need-more", ("amount", $"{shortfall:F2}{CurrencySuffix}"));
        cell.CtaLabel.FontColorOverride = _style.MutedText;
        cell.CtaLabel.Visible = true;
    }

    private void ApplyLocked(RankCell cell, string numeral, int cost)
    {
        cell.Root.StyleBoxOverride = CellBox(_style.LockedBg, _style.LockedBorder);
        cell.Root.Disabled = true;
        cell.RankLabel.Text = Loc.GetString("ranked-upgrade-row-rank-locked", ("rank", numeral));
        cell.RankLabel.FontColorOverride = _style.MutedText;
        cell.CostLabel.Text = FormatCost(cost);
        cell.CostLabel.FontColorOverride = _style.LockedBorder;
        cell.CtaLabel.Visible = false;
    }

    private string FormatCost(int cost)
    {
        return $"{cost}{CurrencySuffix}";
    }

    private static StyleBoxFlat CellBox(Color bg, Color border) => new()
    {
        BackgroundColor = bg,
        BorderColor = border,
        BorderThickness = new Thickness(2),
        ContentMarginLeftOverride = 2,
        ContentMarginRightOverride = 2,
    };

    private PanelContainer MakeSeparator()
    {
        return new PanelContainer
        {
            MinWidth = 2,
            MaxWidth = 2,
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat { BackgroundColor = _style.PanelBorder },
        };
    }

    private static readonly (int Value, string Symbol)[] RomanTable =
    [
        (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
        (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
        (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
    ];

    private static string ToRoman(int value)
    {
        if (value <= 0)
            return value.ToString();

        var result = string.Empty;
        foreach (var (val, symbol) in RomanTable)
        {
            while (value >= val)
            {
                result += symbol;
                value -= val;
            }
        }

        return result;
    }

    private sealed record RankCell(ContainerButton Root, Label RankLabel, Label CostLabel, Label CtaLabel);
}
