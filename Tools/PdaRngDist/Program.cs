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
Console.WriteLine("Tier shares (5-digit curated catalog):");
foreach (PdaRngTier tier in Enum.GetValues<PdaRngTier>())
    Console.WriteLine($"  {tier,-12} {counts[(int)tier],8:N0}  ({counts[(int)tier] * 100.0 / total,6:F2}%)");

Console.WriteLine();
Console.WriteLine("Suggested min XP for top bands:");
foreach (var (label, frac) in new (string, double)[]
{
    ("Notable / top 50%", 0.50),
    ("Irregular / top 25%", 0.75),
    ("Anomalous / top 10%", 0.90),
    ("Classified / top 5%", 0.95),
    ("RealityBend / top 1%", 0.99),
    ("Singularity / top 0.1%", 0.999),
})
{
    var idx = Math.Min(total - 1, (int)(total * frac));
    Console.WriteLine($"  {label,-28} XP >= {xpList[idx]}");
}
