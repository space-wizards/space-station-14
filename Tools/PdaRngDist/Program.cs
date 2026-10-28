using Content.Shared.CartridgeLoader.Cartridges.PdaRng;

var counts = new int[Enum.GetValues<PdaRngTier>().Length];
var xpList = new List<int>(100_000);
long xpSum = 0;

for (var n = 0; n <= 99_999; n++)
{
    var result = PdaRngAnalyzer.Analyze(n);
    counts[(int)result.Tier]++;
    xpList.Add(result.TotalXp);
    xpSum += result.TotalXp;
}

xpList.Sort();
var total = xpList.Count;
Console.WriteLine($"mean={xpSum / (double)total:F2} median={xpList[total / 2]}");
Console.WriteLine("Tier shares (5-digit range):");
foreach (PdaRngTier tier in Enum.GetValues<PdaRngTier>())
    Console.WriteLine($"  {tier,-12} {counts[(int)tier],8:N0}  ({counts[(int)tier] * 100.0 / total,6:F2}%)");

Console.WriteLine();
Console.WriteLine("Example boring / spicy rolls:");
foreach (var n in new[] { 70348, 48291, 25000, 12345, 69, 1, 55555, 13131, 14141 })
{
    var r = PdaRngAnalyzer.Analyze(n);
    Console.WriteLine($"  {n,5} XP={r.TotalXp,3} Tier={r.Tier,-12} pct={r.Percentile} badges={r.Badges.Count}");
}
