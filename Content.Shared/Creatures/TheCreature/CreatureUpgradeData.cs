using System.Collections.Frozen;

namespace Content.Shared.Creatures.TheCreature;

/// <summary>
///     Static definitions for all Creature upgrade tracks.
///     Costs are placeholders — flag for balance pass before ship.
/// </summary>
public sealed record CreatureUpgradeData(
    string Id,
    string Name,
    string Stat,
    int[] Costs,
    string[] Effects)
{
    public const int MaxRank = 3;

    public static readonly IReadOnlyList<CreatureUpgradeData> All = new[]
    {
        new CreatureUpgradeData(
            "predator", "Predator's Strike", "ATTACK",
            new[] { 40, 80, 140 },
            new[]
            {
                "Melee damage +15%.",
                "Melee damage +30%.",
                "Melee damage +50%. Hits apply a brief slow.",
            }),
        new CreatureUpgradeData(
            "quickness", "Quickness", "MOVE",
            new[] { 35, 75, 130 },
            new[]
            {
                "Move speed +8%.",
                "Move speed +16%.",
                "Move speed +25%. Visibility gained per tile reduced.",
            }),
        new CreatureUpgradeData(
            "shadow", "Shadow", "STEALTH",
            new[] { 50, 100, 180 },
            new[]
            {
                "Passive visibility decay +25%.",
                "Passive visibility decay +60%.",
                "Near-instant decay while standing still.",
            }),
        new CreatureUpgradeData(
            "venom", "Venom", "STING",
            new[] { 45, 90, 160 },
            new[]
            {
                "Sting stun +0.8s.",
                "Sting stun +1.6s, sting deals minor damage.",
                "Sting stun +2.4s, applies bleed.",
            }),
        new CreatureUpgradeData(
            "ravenous", "Ravenous", "FEED",
            new[] { 55, 110, 200 },
            new[]
            {
                "Drink Blood channel 20% faster.",
                "Channel 35% faster; heal per tick +25%.",
                "Channel 50% faster; overheal cap added.",
            }),
        new CreatureUpgradeData(
            "pry", "Pry Mastery", "PRY",
            new[] { 30, 70, 150 },
            new[]
            {
                "Door pry 25% faster.",
                "Door pry 60% faster.",
                "Near-instant pry; pry sound suppressed.",
            }),
        new CreatureUpgradeData(
            "ironhide", "Iron Hide", "ARMOR",
            new[] { 50, 100, 170 },
            new[]
            {
                "All damage taken −10%.",
                "All damage taken −20%.",
                "Damage −30%. Drag bodies at full speed.",
            }),
    };

    public static readonly FrozenDictionary<string, CreatureUpgradeData> ById =
        All.ToFrozenDictionary(u => u.Id);
}
