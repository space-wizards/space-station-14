using System.Collections.Frozen;

namespace Content.Shared.Creatures.TheCreature;

/// <summary>
///     Static definitions for all Creature upgrade tracks.
/// </summary>
public sealed record CreatureUpgradeData(
    string Id,
    string Name,
    string Stat,
    int[] Costs,
    string[] Effects)
{
    public const int MaxRank = 3;

    // Each upgrade: 100 / 145 / 185 u = 430 u total
    // 7 upgrades × 430 = 3010 u ≈ 10 full kills to max out
    public static readonly IReadOnlyList<CreatureUpgradeData> All = new[]
    {
        new CreatureUpgradeData(
            "predator", "Strike", "ATTACK",
            new[] { 100, 145, 185 },
            new[]
            {
                "Bite damage +5.",
                "Bite damage +10.",
                "Bite damage +15.",
            }),
        new CreatureUpgradeData(
            "quickness", "Quickness", "MOVE",
            new[] { 100, 145, 185 },
            new[]
            {
                "Move speed +8%.",
                "Move speed +16%.",
                "Move speed +25%.",
            }),
        new CreatureUpgradeData(
            "shadow", "Shadow", "STEALTH",
            new[] { 100, 145, 185 },
            new[]
            {
                "Passive fade speed +33%.",
                "Passive fade speed +80%.",
                "Passive fade speed +153%.",
            }),
        new CreatureUpgradeData(
            "venom", "Venom", "STING",
            new[] { 100, 145, 185 },
            new[]
            {
                "Sting stun 8 seconds.",
                "Sting stun 11 seconds.",
                "Sting stun 14 seconds.",
            }),
        new CreatureUpgradeData(
            "ravenous", "Ravenous", "FEED",
            new[] { 100, 145, 185 },
            new[]
            {
                "Drink Blood channel 20% faster.",
                "Channel 35% faster; heal per tick +25%.",
                "Channel 50% faster; overheal cap added.",
            }),
        new CreatureUpgradeData(
            "pry", "Pry Mastery", "PRY",
            new[] { 100, 145, 185 },
            new[]
            {
                "Door pry 25% faster.",
                "Door pry 60% faster.",
                "Near-instant pry.",
            }),
        new CreatureUpgradeData(
            "ironhide", "Iron Hide", "ARMOR",
            new[] { 100, 145, 185 },
            new[]
            {
                "All damage taken −10%.",
                "All damage taken −20%.",
                "All damage taken −30%.",
            }),
    };

    public static readonly FrozenDictionary<string, CreatureUpgradeData> ById =
        All.ToFrozenDictionary(u => u.Id);
}
