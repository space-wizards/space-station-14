using System.Globalization;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Shared.CartridgeLoader.Cartridges.PdaRng;

/// <summary>
/// Evaluates a rolled number against the badge catalog and builds ticket data.
/// </summary>
public static class PdaRngAnalyzer
{
    public const int MaxPrintedBadges = 20;
    public const int MinNumber = 0;
    public const int MaxNumber = 99_999;

    public static PdaRngRollResult Analyze(int number)
    {
        if (number is < MinNumber or > MaxNumber)
            throw new ArgumentOutOfRangeException(nameof(number), number, "PDA RNG rolls must be in 0..99999");

        var text = number.ToString(CultureInfo.InvariantCulture);
        var badges = new List<PdaRngEarnedBadge>();

        foreach (var badge in PdaRngBadgeCatalog.All)
        {
            if (badge.Matches(number, text))
                badges.Add(new PdaRngEarnedBadge(badge.Id, badge.Xp));
        }

        badges.Sort(static (a, b) =>
        {
            var cmp = b.Xp.CompareTo(a.Xp);
            return cmp != 0 ? cmp : string.CompareOrdinal(a.Id, b.Id);
        });

        var totalXp = 0;
        foreach (var badge in badges)
            totalXp += badge.Xp;

        var (tier, percentile) = PdaRngTierHelper.FromXp(totalXp);
        var (digitSum, unique, odd, even) = ProfileDigits(text);

        return new PdaRngRollResult
        {
            Number = number,
            NumberText = text,
            Badges = badges,
            TotalXp = totalXp,
            Tier = tier,
            Percentile = percentile,
            DigitCount = text.Length,
            DigitSum = digitSum,
            UniqueDigits = unique,
            OddDigitCount = odd,
            EvenDigitCount = even,
        };
    }

    /// <summary>
    /// Builds rich-text paper content for a roll. Caps listed badges at <see cref="MaxPrintedBadges"/>.
    /// </summary>
    public static string FormatTicket(PdaRngRollResult result, string playerName)
    {
        var safeName = FormattedMessage.EscapeText(playerName);
        var tierName = Loc.GetString(PdaRngTierHelper.GetLocId(result.Tier));
        var tierColor = PdaRngTierHelper.GetColor(result.Tier);
        var percentile = result.Percentile.ToString("0.#", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("pda-rng-printout-header"));
        sb.AppendLine();
        sb.AppendLine(Loc.GetString("pda-rng-printout-number", ("number", result.NumberText)));
        sb.AppendLine();

        if (result.Tier == PdaRngTier.Mundane)
        {
            sb.AppendLine(Loc.GetString("pda-rng-printout-tier-bottom",
                ("tier", tierName),
                ("color", tierColor),
                ("percentile", percentile)));
        }
        else
        {
            sb.AppendLine(Loc.GetString("pda-rng-printout-tier-top",
                ("tier", tierName),
                ("color", tierColor),
                ("percentile", percentile)));
        }

        sb.AppendLine(Loc.GetString("pda-rng-printout-total-xp", ("xp", result.TotalXp)));
        sb.AppendLine();
        sb.AppendLine(Loc.GetString("pda-rng-printout-flavor",
            ("digits", result.DigitCount),
            ("sum", result.DigitSum),
            ("unique", result.UniqueDigits),
            ("odd", result.OddDigitCount),
            ("even", result.EvenDigitCount)));
        sb.AppendLine();
        sb.AppendLine(Loc.GetString("pda-rng-printout-badges-header"));

        var printed = Math.Min(result.Badges.Count, MaxPrintedBadges);
        for (var i = 0; i < printed; i++)
        {
            var badge = result.Badges[i];
            var name = Loc.GetString($"pda-rng-badge-{badge.Id}");
            var desc = Loc.GetString($"pda-rng-badge-{badge.Id}.desc");
            sb.AppendLine(Loc.GetString("pda-rng-printout-badge-entry",
                ("name", name),
                ("desc", desc),
                ("xp", badge.Xp)));
        }

        if (result.Badges.Count > MaxPrintedBadges)
        {
            var overflowXp = 0;
            for (var i = MaxPrintedBadges; i < result.Badges.Count; i++)
                overflowXp += result.Badges[i].Xp;

            sb.AppendLine(Loc.GetString("pda-rng-printout-badges-overflow",
                ("count", result.Badges.Count - MaxPrintedBadges),
                ("xp", overflowXp)));
        }
        else if (result.Badges.Count == 0)
        {
            sb.AppendLine(Loc.GetString("pda-rng-printout-badges-none"));
        }

        sb.AppendLine();
        sb.AppendLine(Loc.GetString("pda-rng-printout-issued-to", ("name", safeName)));
        return sb.ToString();
    }

    private static (int Sum, int Unique, int Odd, int Even) ProfileDigits(string text)
    {
        var seen = new bool[10];
        var sum = 0;
        var unique = 0;
        var odd = 0;
        var even = 0;

        foreach (var ch in text)
        {
            var d = ch - '0';
            sum += d;
            if (!seen[d])
            {
                seen[d] = true;
                unique++;
            }

            if ((d & 1) == 1)
                odd++;
            else
                even++;
        }

        return (sum, unique, odd, even);
    }
}
