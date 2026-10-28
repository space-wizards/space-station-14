using System.Globalization;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Shared.CartridgeLoader.Cartridges.PdaRng;

/// <summary>Rarity tier from total badge XP.</summary>
public enum PdaRngTier
{
    Mundane,
    Notable,
    Irregular,
    Anomalous,
    Classified,
    RealityBend,
    Singularity,
}

public readonly record struct PdaRngEarnedBadge(string Id, int Xp);

public sealed class PdaRngRollResult
{
    public required int Number { get; init; }
    public required string NumberText { get; init; }
    public required IReadOnlyList<PdaRngEarnedBadge> Badges { get; init; }
    public required int TotalXp { get; init; }
    public required PdaRngTier Tier { get; init; }
    public required float Percentile { get; init; }
    public required int DigitCount { get; init; }
    public required int DigitSum { get; init; }
    public required int UniqueDigits { get; init; }
    public required int OddDigitCount { get; init; }
    public required int EvenDigitCount { get; init; }
}

/// <summary>
/// Roll analysis, ticket formatting, tier cutoffs, and the badge catalog.
/// Exact/contains/affix badges live as compact tables; pattern badges use predicates.
/// </summary>
public static class PdaRngAnalyzer
{
    public const int MinNumber = 0;
    public const int MaxNumber = 99_999;
    public const int MaxPrintedBadges = 20;

    // Empirically retuned for the curated 5-digit catalog (~top 50/25/10/5/1/0.1%).
    public const int NotableXp = 14;
    public const int IrregularXp = 26;
    public const int AnomalousXp = 52;
    public const int ClassifiedXp = 64;
    public const int RealityBendXp = 99;
    public const int SingularityXp = 180;

    private static readonly IReadOnlyList<Badge> Badges = BuildBadges();

    public static PdaRngRollResult Analyze(int number)
    {
        if (number is < MinNumber or > MaxNumber)
            throw new ArgumentOutOfRangeException(nameof(number), number, "PDA RNG rolls must be in 0..99999");

        var text = number.ToString(CultureInfo.InvariantCulture);
        var earned = new List<PdaRngEarnedBadge>();
        foreach (var badge in Badges)
        {
            if (badge.Matches(number, text))
                earned.Add(new PdaRngEarnedBadge(badge.Id, badge.Xp));
        }

        earned.Sort(static (a, b) =>
        {
            var cmp = b.Xp.CompareTo(a.Xp);
            return cmp != 0 ? cmp : string.CompareOrdinal(a.Id, b.Id);
        });

        var totalXp = 0;
        foreach (var badge in earned)
            totalXp += badge.Xp;

        var (tier, percentile) = TierFromXp(totalXp);
        var (digitSum, unique, odd, even) = ProfileDigits(text);

        return new PdaRngRollResult
        {
            Number = number,
            NumberText = text,
            Badges = earned,
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

    public static string FormatTicket(PdaRngRollResult result, string playerName)
    {
        var safeName = FormattedMessage.EscapeText(playerName);
        var tierName = Loc.GetString(GetTierLocId(result.Tier));
        var tierColor = GetTierColor(result.Tier);
        var percentile = result.Percentile.ToString("0.#", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("pda-rng-printout-header"));
        sb.AppendLine();
        sb.AppendLine(Loc.GetString("pda-rng-printout-number", ("number", result.NumberText)));
        sb.AppendLine();

        sb.AppendLine(Loc.GetString(
            result.Tier == PdaRngTier.Mundane ? "pda-rng-printout-tier-bottom" : "pda-rng-printout-tier-top",
            ("tier", tierName),
            ("color", tierColor),
            ("percentile", percentile)));

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
            sb.AppendLine(Loc.GetString("pda-rng-printout-badge-entry",
                ("name", Loc.GetString($"pda-rng-badge-{badge.Id}")),
                ("desc", Loc.GetString($"pda-rng-badge-{badge.Id}.desc")),
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

    public static (PdaRngTier Tier, float Percentile) TierFromXp(int totalXp)
    {
        return totalXp switch
        {
            >= SingularityXp => (PdaRngTier.Singularity, 0.1f),
            >= RealityBendXp => (PdaRngTier.RealityBend, 1f),
            >= ClassifiedXp => (PdaRngTier.Classified, 5f),
            >= AnomalousXp => (PdaRngTier.Anomalous, 10f),
            >= IrregularXp => (PdaRngTier.Irregular, 25f),
            >= NotableXp => (PdaRngTier.Notable, 50f),
            _ => (PdaRngTier.Mundane, Math.Clamp(99f - totalXp * (49f / Math.Max(1, NotableXp - 1)), 50f, 99f)),
        };
    }

    public static string GetTierLocId(PdaRngTier tier) => tier switch
    {
        PdaRngTier.Mundane => "pda-rng-tier-mundane",
        PdaRngTier.Notable => "pda-rng-tier-notable",
        PdaRngTier.Irregular => "pda-rng-tier-irregular",
        PdaRngTier.Anomalous => "pda-rng-tier-anomalous",
        PdaRngTier.Classified => "pda-rng-tier-classified",
        PdaRngTier.RealityBend => "pda-rng-tier-reality-bend",
        PdaRngTier.Singularity => "pda-rng-tier-singularity",
        _ => "pda-rng-tier-mundane",
    };

    public static string GetTierColor(PdaRngTier tier) => tier switch
    {
        PdaRngTier.Mundane => "#8a8a8a",
        PdaRngTier.Notable => "#4caf50",
        PdaRngTier.Irregular => "#2196f3",
        PdaRngTier.Anomalous => "#9c27b0",
        PdaRngTier.Classified => "#ff9800",
        PdaRngTier.RealityBend => "#f44336",
        PdaRngTier.Singularity => "#e91e63",
        _ => "#8a8a8a",
    };

    private readonly record struct Badge(string Id, int Xp, Func<int, string, bool> Matches);

    private static IReadOnlyList<Badge> BuildBadges()
    {
        var badges = new List<Badge>(80);

        // Digit length
        AddLen(badges, "OneDigit", 1, 120);
        AddLen(badges, "TwoDigits", 2, 80);
        AddLen(badges, "ThreeDigits", 3, 40);
        AddLen(badges, "FourDigits", 4, 15);
        AddLen(badges, "FiveDigits", 5, 1);

        // Math / properties
        badges.Add(new("Even", 1, static (n, _) => n % 2 == 0));
        badges.Add(new("Odd", 1, static (n, _) => n % 2 != 0));
        badges.Add(new("DivisibleBy3", 2, static (n, _) => n % 3 == 0));
        badges.Add(new("DivisibleBy5", 3, static (n, _) => n % 5 == 0));
        badges.Add(new("DivisibleBy7", 4, static (n, _) => n % 7 == 0));
        badges.Add(new("DivisibleBy13", 10, static (n, _) => n % 13 == 0));
        badges.Add(new("DivisibleBy14", 8, static (n, _) => n % 14 == 0));
        badges.Add(new("Prime", 45, static (n, _) => IsPrime(n)));
        badges.Add(new("PerfectSquare", 55, static (n, _) => IsPerfectPower(n, 2)));
        badges.Add(new("PerfectCube", 70, static (n, _) => IsPerfectPower(n, 3)));
        badges.Add(new("Fibonacci", 60, static (n, _) => IsFibonacci(n)));
        badges.Add(new("PowerOfTwo", 50, static (n, _) => n > 0 && (n & (n - 1)) == 0));

        // Digit patterns
        badges.Add(new("Palindrome", 35, static (_, s) => IsPalindrome(s)));
        badges.Add(new("AllSameDigits", 85, static (_, s) => s.Length >= 2 && AllSame(s)));
        badges.Add(new("AllUniqueDigits", 8, static (_, s) => AllUnique(s)));
        badges.Add(new("StrictlyAscending", 70, static (_, s) => IsStrictMono(s, up: true)));
        badges.Add(new("StrictlyDescending", 70, static (_, s) => IsStrictMono(s, up: false)));
        badges.Add(new("Doublet", 4, static (_, s) => HasRun(s, 2)));
        badges.Add(new("Triplet", 28, static (_, s) => HasRun(s, 3)));
        badges.Add(new("RepeatingBlock", 55, static (_, s) => IsRepeatingBlock(s)));
        badges.Add(new("ConsecutiveDigram", 28, static (_, s) => HasConsecutiveDigram(s)));
        badges.Add(new("BinaryLooking", 30, static (_, s) => OnlyDigits(s, "01")));
        badges.Add(new("ZeroFree", 1, static (_, s) => !s.Contains('0')));
        badges.Add(new("Sandwich", 10, static (_, s) => s.Length >= 3 && s[0] == s[^1]));

        // Exact values — (id, number, xp)
        (string Id, int Number, int Xp)[] exacts =
        [
            ("ZeroHero", 0, 130),
            ("SingularityOne", 1, 110),
            ("UnluckyThirteen", 13, 35),
            ("SpaceLawFourteen", 14, 40),
            ("AnswerToEverything", 42, 35),
            ("SixtySeven", 67, 35),
            ("Nice", 69, 40),
            ("BlazeIt", 420, 55),
            ("Beast", 666, 60),
            ("JackpotSevens", 777, 50),
            ("Emergency911", 911, 45),
            ("ThirteenThirteen", 1313, 55),
            ("FourteenFourteen", 1414, 55),
            ("Leet", 1337, 70),
            ("NukeAdjacent", 1984, 60),
            ("Sequence12345", 12345, 55),
            ("ThirteenCascade", 13131, 90),
            ("FourteenCascade", 14141, 90),
            ("FourteenK", 14000, 45),
            ("PiLite", 31415, 90),
            ("Sequence54321", 54321, 85),
            ("AllFives", 55555, 110),
            ("HereticMark", 66666, 110),
            ("NiceBlaze", 69420, 95),
            ("BlazeNice", 42069, 95),
            ("QuadSevens", 77777, 120),
            ("BoobiesPlus", 80085, 90),
            ("PrimeNearEnd", 99991, 130),
            ("MaxRoll", 99999, 120),
        ];
        foreach (var (id, number, xp) in exacts)
            badges.Add(new(id, xp, (n, _) => n == number));

        // Substring contains — (id, needle, xp)
        (string Id, string Needle, int Xp)[] contains =
        [
            ("Contains13", "13", 5),
            ("Contains14", "14", 5),
            ("Contains67", "67", 8),
            ("Contains69", "69", 8),
            ("Contains420", "420", 14),
            ("Contains666", "666", 35),
            ("Contains777", "777", 35),
            ("Contains007", "007", 40),
        ];
        foreach (var (id, needle, xp) in contains)
            badges.Add(new(id, xp, (_, s) => s.Contains(needle, StringComparison.Ordinal)));

        // Affixes — (id, value, xp, starts)
        (string Id, string Value, int Xp, bool Starts)[] affixes =
        [
            ("StartsWith13", "13", 12, true),
            ("EndsWith13", "13", 12, false),
            ("StartsWith14", "14", 12, true),
            ("EndsWith14", "14", 12, false),
            ("StartsWith69", "69", 35, true),
            ("EndsWith69", "69", 35, false),
        ];
        foreach (var (id, value, xp, starts) in affixes)
        {
            badges.Add(starts
                ? new(id, xp, (_, s) => s.StartsWith(value, StringComparison.Ordinal))
                : new(id, xp, (_, s) => s.EndsWith(value, StringComparison.Ordinal)));
        }

        var ids = new HashSet<string>();
        foreach (var badge in badges)
        {
            if (!ids.Add(badge.Id))
                throw new InvalidOperationException($"Duplicate PDA RNG badge id: {badge.Id}");
        }

        return badges;
    }

    private static void AddLen(List<Badge> badges, string id, int length, int xp) =>
        badges.Add(new(id, xp, (_, s) => s.Length == length));

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

    private static bool IsPrime(int n)
    {
        if (n < 2)
            return false;
        if (n % 2 == 0)
            return n == 2;
        for (var i = 3; i * i <= n; i += 2)
        {
            if (n % i == 0)
                return false;
        }

        return true;
    }

    private static bool IsPerfectPower(int n, int power)
    {
        if (n < 0)
            return false;
        if (n <= 1)
            return true;
        var rounded = (int)Math.Round(Math.Pow(n, 1.0 / power));
        long value = 1;
        for (var i = 0; i < power; i++)
            value *= rounded;
        return value == n;
    }

    private static bool IsFibonacci(int n)
    {
        if (n < 0)
            return false;
        long x = n;
        return IsPerfectSquare(5 * x * x + 4) || IsPerfectSquare(5 * x * x - 4);
    }

    private static bool IsPerfectSquare(long n)
    {
        if (n < 0)
            return false;
        var r = (long)Math.Round(Math.Sqrt(n));
        return r * r == n;
    }

    private static bool IsPalindrome(string s)
    {
        for (var i = 0; i < s.Length / 2; i++)
        {
            if (s[i] != s[^(i + 1)])
                return false;
        }

        return true;
    }

    private static bool AllSame(string s)
    {
        for (var i = 1; i < s.Length; i++)
        {
            if (s[i] != s[0])
                return false;
        }

        return true;
    }

    private static bool AllUnique(string s)
    {
        var seen = 0;
        foreach (var ch in s)
        {
            var bit = 1 << (ch - '0');
            if ((seen & bit) != 0)
                return false;
            seen |= bit;
        }

        return true;
    }

    private static bool IsStrictMono(string s, bool up)
    {
        if (s.Length < 2)
            return false;
        for (var i = 1; i < s.Length; i++)
        {
            if (up ? s[i] <= s[i - 1] : s[i] >= s[i - 1])
                return false;
        }

        return true;
    }

    private static bool HasRun(string s, int runLength)
    {
        if (s.Length < runLength)
            return false;
        var run = 1;
        for (var i = 1; i < s.Length; i++)
        {
            run = s[i] == s[i - 1] ? run + 1 : 1;
            if (run >= runLength)
                return true;
        }

        return false;
    }

    private static bool IsRepeatingBlock(string s)
    {
        if (s.Length < 2 || s.Length % 2 != 0)
            return false;
        var half = s.Length / 2;
        return s.AsSpan(0, half).SequenceEqual(s.AsSpan(half));
    }

    private static bool HasConsecutiveDigram(string s)
    {
        for (var i = 0; i <= s.Length - 4; i++)
        {
            if (s[i] == s[i + 2] && s[i + 1] == s[i + 3])
                return true;
        }

        return false;
    }

    private static bool OnlyDigits(string s, string allowed)
    {
        foreach (var ch in s)
        {
            if (!allowed.Contains(ch))
                return false;
        }

        return true;
    }
}
