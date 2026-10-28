using System.Linq;

namespace Content.Shared.CartridgeLoader.Cartridges.PdaRng;

/// <summary>
/// Initial set of exactly 203 PDA RNG badges.
/// Ids are stable locale suffixes (<c>pda-rng-badge-{Id}</c>). Prefer appending new badges at the end.
/// </summary>
public static class PdaRngBadgeCatalog
{
    public static IReadOnlyList<PdaRngBadge> All { get; } = Build();

    private static IReadOnlyList<PdaRngBadge> Build()
    {
        PdaRngBadge[] badges =
        [
            // Digit length (5) — max roll is 99999
            Len("OneDigit", 1, 120),
            Len("TwoDigits", 2, 80),
            Len("ThreeDigits", 3, 40),
            Len("FourDigits", 4, 15),
            Len("FiveDigits", 5, 1),

            // Basic math (18)
            Pred("Even", 1, static (n, _) => n % 2 == 0),
            Pred("Odd", 1, static (n, _) => n % 2 != 0),
            Pred("DivisibleBy3", 2, static (n, _) => n % 3 == 0),
            Pred("DivisibleBy5", 3, static (n, _) => n % 5 == 0),
            Pred("DivisibleBy7", 4, static (n, _) => n % 7 == 0),
            Pred("DivisibleBy9", 4, static (n, _) => n % 9 == 0),
            Pred("DivisibleBy11", 6, static (n, _) => n % 11 == 0),
            Pred("DivisibleBy13", 10, static (n, _) => n % 13 == 0),
            Pred("DivisibleBy14", 8, static (n, _) => n % 14 == 0),
            Pred("DivisibleBy25", 10, static (n, _) => n % 25 == 0),
            Pred("DivisibleBy100", 20, static (n, _) => n % 100 == 0),
            Pred("DivisibleBy1000", 35, static (n, _) => n % 1000 == 0),
            Pred("Prime", 45, static (n, _) => IsPrime(n)),
            Pred("Composite", 1, static (n, _) => n > 3 && !IsPrime(n)),
            Pred("PerfectSquare", 55, static (n, _) => IsPerfectPower(n, 2)),
            Pred("PerfectCube", 70, static (n, _) => IsPerfectPower(n, 3)),
            Pred("PerfectFourth", 90, static (n, _) => IsPerfectPower(n, 4)),
            Pred("PowerOfTwo", 50, static (n, _) => n > 0 && (n & (n - 1)) == 0),

            // Special sets (12)
            Pred("Fibonacci", 60, static (n, _) => IsFibonacci(n)),
            Pred("Triangular", 40, static (n, _) => IsTriangular(n)),
            Pred("Factorial", 100, static (n, _) => IsFactorial(n)),
            Pred("PowerOfTen", 75, static (n, _) => IsPowerOfTen(n)),
            Pred("PowerOfFive", 45, static (n, _) => IsPowerOfFive(n)),
            Pred("Harshad", 8, static (n, s) => n > 0 && n % DigitSum(s) == 0),
            Pred("DigitalRootNine", 3, static (_, s) => DigitalRoot(s) == 9),
            Pred("DigitalRootOne", 3, static (_, s) => DigitalRoot(s) == 1),
            Pred("Automorphic", 110, static (n, s) => IsAutomorphic(n, s)),
            Pred("Kaprekarish", 35, static (n, s) => s.Length >= 3 && IsKaprekarish(n)),
            Pred("Repunit", 95, static (_, s) => s.Length >= 2 && s.All(c => c == '1')),
            Pred("BinaryLooking", 30, static (_, s) => s.All(c => c is '0' or '1')),

            // Digit patterns (30)
            Pred("Palindrome", 35, static (_, s) => IsPalindrome(s)),
            Pred("AllSameDigits", 85, static (_, s) => s.Length >= 2 && s.All(c => c == s[0])),
            Pred("AllUniqueDigits", 8, static (_, s) => s.Distinct().Count() == s.Length),
            Pred("StrictlyAscending", 70, static (_, s) => IsStrictlyMonotonic(s, ascending: true)),
            Pred("StrictlyDescending", 70, static (_, s) => IsStrictlyMonotonic(s, ascending: false)),
            Pred("NonDecreasing", 18, static (_, s) => IsMonotonic(s, ascending: true)),
            Pred("NonIncreasing", 18, static (_, s) => IsMonotonic(s, ascending: false)),
            Pred("AlternatingParityDigits", 22, static (_, s) => IsAlternatingParity(s)),
            Pred("Doublet", 4, static (_, s) => HasRun(s, 2)),
            Pred("Triplet", 28, static (_, s) => HasRun(s, 3)),
            Pred("Quadruplet", 80, static (_, s) => HasRun(s, 4)),
            Pred("TwoPairPattern", 40, static (_, s) => IsTwoPairPattern(s)),
            Pred("RepeatingBlock", 55, static (_, s) => IsRepeatingBlock(s)),
            Pred("MirrorHalves", 50, static (_, s) => IsMirrorHalves(s)),
            Pred("ContainsZero", 1, static (_, s) => s.Contains('0')),
            Pred("ZeroFree", 1, static (_, s) => !s.Contains('0')),
            Pred("ContainsOne", 1, static (_, s) => s.Contains('1')),
            Pred("ContainsFour", 1, static (_, s) => s.Contains('4')),
            Pred("ContainsSeven", 1, static (_, s) => s.Contains('7')),
            Pred("ContainsEight", 1, static (_, s) => s.Contains('8')),
            Pred("StartsWithOne", 2, static (_, s) => s[0] == '1'),
            Pred("EndsWithZero", 3, static (_, s) => s[^1] == '0'),
            Pred("EndsWithSeven", 3, static (_, s) => s[^1] == '7'),
            Pred("LowDigitSum", 22, static (_, s) => DigitSum(s) <= 5),
            Pred("HighDigitSum", 22, static (_, s) => DigitSum(s) >= 35),
            Pred("DigitSumLucky7", 8, static (_, s) => DigitSum(s) == 7),
            Pred("DigitSum14", 8, static (_, s) => DigitSum(s) == 14),
            Pred("MostlyOddDigits", 3, static (_, s) => CountOddDigits(s) > s.Length / 2),
            Pred("MostlyEvenDigits", 3, static (_, s) => CountOddDigits(s) * 2 < s.Length),
            Pred("OnlyOddDigits", 35, static (_, s) => s.All(c => (c - '0') % 2 == 1)),

            // Meme / culture (28)
            Exact("Nice", 69, 40),
            Exact("BlazeIt", 420, 55),
            Exact("Beast", 666, 60),
            Exact("JackpotSevens", 777, 50),
            Exact("CrazyEights", 888, 50),
            Exact("AlmostThere", 999, 45),
            Exact("Leet", 1337, 70),
            Exact("Boobies", 8008, 65),
            Exact("BoobiesPlus", 80085, 90),
            Exact("Sequence1234", 1234, 35),
            Exact("Sequence12345", 12345, 55),
            Exact("Sequence54321", 54321, 85),
            Exact("NiceBlaze", 69420, 95),
            Exact("BlazeNice", 42069, 95),
            Exact("DoubleNice", 6969, 70),
            Exact("PiLite", 31415, 90),
            Exact("EulerLite", 27182, 90),
            Exact("AnswerToEverything", 42, 35),
            Exact("NotFound", 404, 40),
            Exact("OkStatus", 200, 25),
            Exact("ServerError", 500, 30),
            Exact("JennyLite", 86753, 75),
            Exact("BinaryPulse", 10101, 55),
            Exact("HalfAndHalf", 11000, 50),
            Exact("MaxRoll", 99999, 120),
            Exact("ZeroHero", 0, 130),

            // Station / SS14 (28)
            Exact("UnluckyThirteen", 13, 35),
            Exact("SpaceLawFourteen", 14, 40),
            Exact("ThirteenThirteen", 1313, 55),
            Exact("FourteenFourteen", 1414, 55),
            Exact("ThirteenCascade", 13131, 90),
            Exact("FourteenCascade", 14141, 90),
            Exact("FourteenHundred", 1400, 35),
            Exact("FourteenK", 14000, 45),
            Exact("ThirteenFourteen", 1314, 45),
            Exact("Emergency911", 911, 45),
            Exact("Medical99", 99, 30),
            Contains("Contains69", "69", 8),
            Contains("Contains420", "420", 14),
            Contains("Contains666", "666", 35),
            Contains("Contains1337", "1337", 45),
            Contains("Contains14", "14", 5),
            Contains("Contains13", "13", 5),
            Contains("Contains007", "007", 40),
            StartsWith("StartsWith14", "14", 12),
            EndsWith("EndsWith14", "14", 12),
            StartsWith("StartsWith13", "13", 12),
            EndsWith("EndsWith13", "13", 12),
            Exact("RobustPower", 256, 40),
            Exact("RobustKilo", 1024, 50),
            Exact("CargoTwenty", 20, 20),
            Exact("AtmosThirtyFour", 34, 25),
            Exact("NukeAdjacent", 1984, 60),
            Exact("SingularityOne", 1, 110),

            // More patterns + lore (28) — total so far 6+18+12+30+28+28 = 122, need 28 more
            Pred("OnlyEvenDigits", 35, static (_, s) => s.All(c => (c - '0') % 2 == 0)),
            Pred("TernaryLooking", 40, static (_, s) => s.All(c => c is '0' or '1' or '2')),
            Pred("UsesFourDistinct", 2, static (_, s) => s.Distinct().Count() == 4),
            Pred("UsesFiveDistinct", 3, static (_, s) => s.Length >= 5 && s.Distinct().Count() == 5),
            Pred("UsesTwoDistinct", 12, static (_, s) => s.Distinct().Count() == 2),
            Pred("UsesThreeDistinct", 4, static (_, s) => s.Distinct().Count() == 3),
            Pred("SymmetricPairs", 25, static (_, s) => HasSymmetricPairs(s)),
            Pred("Sandwich", 10, static (_, s) => s.Length >= 3 && s[0] == s[^1]),
            Contains("Contains321", "321", 10),
            Contains("Contains111", "111", 30),
            Contains("Contains777", "777", 35),
            Contains("Contains000", "000", 40),
            StartsWith("StartsWith69", "69", 35),
            EndsWith("EndsWith69", "69", 35),
            StartsWith("StartsWith420", "420", 45),
            EndsWith("EndsWith420", "420", 45),
            Exact("LuckySevensPair", 77, 25),
            Exact("DoctorNumber", 101, 25),
            Exact("Gross", 144, 30),
            Exact("Mirror12321", 12321, 70),
            Exact("PrimeNearEnd", 99991, 130),
            Exact("WizardSeven", 7, 40),
            Exact("SmallPrimeThree", 3, 50),
            Exact("SmallPrimeFive", 5, 50),
            Exact("BrigTime", 600, 25),
            Exact("Permabrig", 9999, 55),
            Exact("HereticMark", 66666, 110),
            Exact("AllFives", 55555, 110),
            Exact("QuadSevens", 77777, 120),
            Exact("Repeater12121", 12121, 70),
            Exact("NinetyK", 90000, 55),
            Exact("FiftyK", 50000, 45),

            // Batch 2 — 50 more badges (total 200)
            // Math / properties (10)
            Pred("DivisibleBy15", 4, static (n, _) => n % 15 == 0),
            Pred("DivisibleBy16", 7, static (n, _) => n % 16 == 0),
            Pred("DivisibleBy17", 12, static (n, _) => n % 17 == 0),
            Pred("DivisibleBy21", 7, static (n, _) => n % 21 == 0),
            Pred("DivisibleBy42", 18, static (n, _) => n % 42 == 0),
            Pred("DivisibleBy69", 22, static (n, _) => n % 69 == 0),
            Pred("PerfectFifth", 100, static (n, _) => IsPerfectPower(n, 5)),
            Pred("PowerOfThree", 55, static (n, _) => IsPowerOfThree(n)),
            Pred("SparseDigits", 8, static (_, s) => DigitSum(s) <= s.Length * 2),
            Pred("DigitSumEqualsLength", 30, static (_, s) => DigitSum(s) == s.Length),

            // Digit patterns (14)
            Pred("StartsAndEndsSamePair", 28, static (_, s) => s.Length >= 4 && s[..2] == s[^2..]),
            Pred("CenteredDoublet", 12, static (_, s) => s.Length >= 4 && s[s.Length / 2 - 1] == s[s.Length / 2]),
            Pred("NoConsecutiveDigits", 5, static (_, s) => s.Length >= 2 && !HasRun(s, 2)),
            Pred("HasAscendingPair", 2, static (_, s) => HasAdjacentStep(s, 1)),
            Pred("HasDescendingPair", 2, static (_, s) => HasAdjacentStep(s, -1)),
            Pred("ContainsConsecutiveRun3", 18, static (_, s) => HasConsecutiveRun(s, 3)),
            Pred("FirstDigitEven", 1, static (_, s) => (s[0] - '0') % 2 == 0),
            Pred("LastDigitOdd", 1, static (_, s) => (s[^1] - '0') % 2 == 1),
            Pred("AllDigitsLessThanFive", 40, static (_, s) => s.All(c => c < '5')),
            Pred("AllDigitsAtLeastFive", 40, static (_, s) => s.All(c => c >= '5')),
            Pred("AlternatingSamePair", 55, static (_, s) => IsAlternatingSamePair(s)),
            Pred("MirrorEnds", 12, static (_, s) => s.Length >= 4 && s[0] == s[^1] && s[1] == s[^2]),
            Pred("IncreasingPairs", 45, static (_, s) => IsIncreasingPairs(s)),
            Pred("BalancedParityDigits", 5, static (_, s) => s.Length >= 2 && CountOddDigits(s) * 2 == s.Length),

            // Exact culture / station / calendar (14)
            Exact("OverNineThousand", 9001, 80),
            Exact("BinaryMaxByte", 255, 35),
            Exact("KilobyteAdjacent", 1023, 40),
            Exact("Year2000", 2000, 30),
            Exact("Year2024", 2024, 35),
            Exact("Lucky888", 8888, 60),
            Exact("UnluckyFourFour", 4444, 55),
            Exact("SecurityTwentyFive", 25, 25),
            Exact("ClownHonk", 360, 30),
            Exact("CaptainOneFive", 15, 30),
            Exact("EngineerThirty", 30, 25),
            Exact("LeapDay", 229, 40),
            Exact("Halloween", 1031, 40),

            // Contains / affixes (12)
            Contains("Contains88", "88", 6),
            Contains("Contains99", "99", 6),
            Contains("Contains123", "123", 8),
            Contains("Contains7777", "7777", 70),
            Contains("Contains42", "42", 5),
            Contains("Contains911", "911", 35),
            StartsWith("StartsWith77", "77", 14),
            EndsWith("EndsWith77", "77", 14),
            StartsWith("StartsWith88", "88", 14),
            EndsWith("EndsWith88", "88", 14),
            StartsWith("StartsWith99", "99", 14),
            EndsWith("EndsWith99", "99", 14),

            // Digram / meme follow-ups (777 exact+contains and 007 already exist above)
            Exact("SixtySeven", 67, 35),
            Contains("Contains67", "67", 8),
            // e.g. 257373 → "73" "73" back-to-back
            Pred("ConsecutiveDigram", 28, static (_, s) => HasConsecutiveDigram(s)),
        ];

        if (badges.Length != 203)
            throw new InvalidOperationException($"PDA RNG badge catalog must contain exactly 203 badges, got {badges.Length}.");

        var ids = new HashSet<string>();
        foreach (var badge in badges)
        {
            if (!ids.Add(badge.Id))
                throw new InvalidOperationException($"Duplicate PDA RNG badge id: {badge.Id}");
        }

        return badges;
    }

    private static PdaRngBadge Len(string id, int length, int xp) =>
        Pred(id, xp, (_, s) => s.Length == length);

    private static PdaRngBadge Pred(string id, int xp, Func<int, string, bool> matches) =>
        new(id, xp, matches);

    private static PdaRngBadge Exact(string id, int number, int xp) =>
        Pred(id, xp, (n, _) => n == number);

    private static PdaRngBadge Contains(string id, string needle, int xp) =>
        Pred(id, xp, (_, s) => s.Contains(needle, StringComparison.Ordinal));

    private static PdaRngBadge StartsWith(string id, string prefix, int xp) =>
        Pred(id, xp, (_, s) => s.StartsWith(prefix, StringComparison.Ordinal));

    private static PdaRngBadge EndsWith(string id, string suffix, int xp) =>
        Pred(id, xp, (_, s) => s.EndsWith(suffix, StringComparison.Ordinal));

    private static int DigitSum(string s)
    {
        var sum = 0;
        foreach (var c in s)
            sum += c - '0';
        return sum;
    }

    private static int DigitalRoot(string s)
    {
        var n = DigitSum(s);
        while (n >= 10)
        {
            var t = 0;
            while (n > 0)
            {
                t += n % 10;
                n /= 10;
            }

            n = t;
        }

        return n;
    }

    private static int CountOddDigits(string s)
    {
        var count = 0;
        foreach (var c in s)
        {
            if (((c - '0') & 1) == 1)
                count++;
        }

        return count;
    }

    private static bool IsPrime(int n)
    {
        if (n < 2)
            return false;
        if (n % 2 == 0)
            return n == 2;
        var limit = (int)Math.Sqrt(n);
        for (var i = 3; i <= limit; i += 2)
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
        var root = Math.Pow(n, 1.0 / power);
        var rounded = (int)Math.Round(root);
        var value = 1L;
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

    private static bool IsTriangular(int n) => IsPerfectSquare(8L * n + 1);

    private static bool IsFactorial(int n)
    {
        if (n < 1)
            return false;
        var fact = 1;
        for (var i = 1; ; i++)
        {
            if (fact == n)
                return true;
            if (fact > n || fact > int.MaxValue / (i + 1))
                return false;
            fact *= i + 1;
        }
    }

    private static bool IsPowerOfTen(int n)
    {
        if (n <= 0)
            return false;
        while (n % 10 == 0)
            n /= 10;
        return n == 1;
    }

    private static bool IsPowerOfFive(int n)
    {
        if (n <= 0)
            return false;
        while (n % 5 == 0)
            n /= 5;
        return n == 1;
    }

    private static bool IsPowerOfThree(int n)
    {
        if (n <= 0)
            return false;
        while (n % 3 == 0)
            n /= 3;
        return n == 1;
    }

    private static bool HasAdjacentStep(string s, int step)
    {
        for (var i = 1; i < s.Length; i++)
        {
            if (s[i] - s[i - 1] == step)
                return true;
        }

        return false;
    }

    private static bool HasConsecutiveRun(string s, int runLength)
    {
        if (s.Length < runLength)
            return false;

        for (var i = 0; i <= s.Length - runLength; i++)
        {
            var ascending = true;
            var descending = true;
            for (var j = 1; j < runLength; j++)
            {
                if (s[i + j] - s[i + j - 1] != 1)
                    ascending = false;
                if (s[i + j] - s[i + j - 1] != -1)
                    descending = false;
            }

            if (ascending || descending)
                return true;
        }

        return false;
    }

    /// <summary>
    /// True if any two-digit block repeats immediately, e.g. 7373 or 1212 inside a larger number.
    /// </summary>
    private static bool HasConsecutiveDigram(string s)
    {
        for (var i = 0; i <= s.Length - 4; i++)
        {
            if (s[i] == s[i + 2] && s[i + 1] == s[i + 3])
                return true;
        }

        return false;
    }

    private static bool IsAlternatingSamePair(string s)
    {
        if (s.Length < 4 || s.Length % 2 != 0)
            return false;

        var a = s[0];
        var b = s[1];
        if (a == b)
            return false;

        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] != (i % 2 == 0 ? a : b))
                return false;
        }

        return true;
    }

    private static bool IsIncreasingPairs(string s)
    {
        if (s.Length < 4 || s.Length % 2 != 0)
            return false;

        var prev = -1;
        for (var i = 0; i < s.Length; i += 2)
        {
            var pair = (s[i] - '0') * 10 + (s[i + 1] - '0');
            if (pair <= prev)
                return false;
            prev = pair;
        }

        return true;
    }

    private static bool IsAutomorphic(int n, string s)
    {
        long sq = (long)n * n;
        return sq.ToString().EndsWith(s, StringComparison.Ordinal);
    }

    private static bool IsKaprekarish(int n)
    {
        long sq = (long)n * n;
        var sqText = sq.ToString();
        for (var i = 1; i < sqText.Length; i++)
        {
            var left = long.Parse(sqText[..i]);
            var right = long.Parse(sqText[i..]);
            if (right > 0 && left + right == n)
                return true;
        }

        return false;
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

    private static bool IsStrictlyMonotonic(string s, bool ascending)
    {
        if (s.Length < 2)
            return false;
        for (var i = 1; i < s.Length; i++)
        {
            if (ascending)
            {
                if (s[i] <= s[i - 1])
                    return false;
            }
            else if (s[i] >= s[i - 1])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsMonotonic(string s, bool ascending)
    {
        if (s.Length < 2)
            return false;
        for (var i = 1; i < s.Length; i++)
        {
            if (ascending)
            {
                if (s[i] < s[i - 1])
                    return false;
            }
            else if (s[i] > s[i - 1])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAlternatingParity(string s)
    {
        if (s.Length < 2)
            return false;
        for (var i = 1; i < s.Length; i++)
        {
            var prevOdd = ((s[i - 1] - '0') & 1) == 1;
            var curOdd = ((s[i] - '0') & 1) == 1;
            if (prevOdd == curOdd)
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
            if (s[i] == s[i - 1])
            {
                run++;
                if (run >= runLength)
                    return true;
            }
            else
            {
                run = 1;
            }
        }

        return false;
    }

    private static bool IsTwoPairPattern(string s) =>
        s.Length == 4 && s[0] == s[1] && s[2] == s[3] && s[0] != s[2];

    private static bool IsRepeatingBlock(string s)
    {
        if (s.Length < 2 || s.Length % 2 != 0)
            return false;
        var half = s.Length / 2;
        return s[..half] == s[half..];
    }

    private static bool IsMirrorHalves(string s)
    {
        if (s.Length < 2 || s.Length % 2 != 0)
            return false;
        var half = s.Length / 2;
        var left = s[..half];
        var right = s[half..];
        for (var i = 0; i < half; i++)
        {
            if (left[i] != right[^(i + 1)])
                return false;
        }

        return true;
    }

    private static bool HasSymmetricPairs(string s)
    {
        if (s.Length < 4)
            return false;
        var hits = 0;
        for (var i = 0; i < s.Length / 2; i++)
        {
            if (s[i] == s[^(i + 1)])
                hits++;
        }

        return hits >= 2;
    }
}
