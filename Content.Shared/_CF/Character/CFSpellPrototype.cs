using Content.Shared.CF.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CF.Character;

[Serializable, NetSerializable]
public enum CFSpellTradition
{
    Arcane,
    Divine,
    Occult,
    Primal,
}

[Serializable, NetSerializable]
public enum CFSpellActionCost
{
    One,
    Two,
    Three,
    Reaction,
    Free,
    /// <summary>The spell can be cast with one, two, or three actions.</summary>
    Variable,
}

/// <summary>
/// One Pathfinder spell. Text fields are short mechanical notes, not source-book prose.
/// </summary>
[Prototype("cfSpell")]
public sealed partial class CFSpellPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = "";

    /// <summary>0 is a cantrip.</summary>
    [DataField]
    public int Rank;

    [DataField]
    public List<CFSpellTradition> Traditions = new();

    [DataField]
    public CFRarity Rarity = CFRarity.Common;

    [DataField]
    public List<ProtoId<CFTraitPrototype>> Traits = new();

    [DataField]
    public CFSpellActionCost ActionCost = CFSpellActionCost.Two;

    [DataField]
    public string Components = "";

    [DataField]
    public string Range = "";

    [DataField]
    public string Area = "";

    [DataField]
    public string Targets = "";

    [DataField]
    public string Duration = "";

    /// <summary>Save or attack, such as "Reflex" or "Spell attack".</summary>
    [DataField]
    public string Defense = "";

    [DataField]
    public string Summary = "";

    [DataField]
    public string CriticalSuccess = "";

    [DataField]
    public string Success = "";

    [DataField]
    public string Failure = "";

    [DataField]
    public string CriticalFailure = "";

    [DataField]
    public string Heightened = "";

    public bool HasTradition(CFSpellTradition tradition)
    {
        return Traditions.Contains(tradition);
    }
}

public readonly record struct CFSpellRankSlots(int Rank, int Count);

public static class CFSpellSlots
{
    /// <summary>Sheet shown until a class defines its own spellcasting.</summary>
    public static readonly CFSpellRankSlots[] Preview =
    {
        new(0, 5),
        new(1, 3),
        new(2, 2),
        new(3, 2),
    };

    public const int BonusRank = -1;

    public static IReadOnlyList<CFSpellRankSlots> ForClass(CFClassPrototype? cls)
    {
        var casting = cls?.Spellcasting;
        if (casting == null)
            return Preview;

        var ranks = new List<CFSpellRankSlots>();
        if (casting.Cantrips > 0)
            ranks.Add(new CFSpellRankSlots(0, casting.Cantrips));

        for (var i = 0; i < casting.Slots.Count; i++)
        {
            if (casting.Slots[i] > 0)
                ranks.Add(new CFSpellRankSlots(i + 1, casting.Slots[i]));
        }

        return ranks;
    }

    public static string ActionSymbols(CFSpellActionCost cost)
    {
        return cost switch
        {
            CFSpellActionCost.One => "◆",
            CFSpellActionCost.Two => "◆◆",
            CFSpellActionCost.Three => "◆◆◆",
            CFSpellActionCost.Reaction => "↺",
            CFSpellActionCost.Free => "◇",
            CFSpellActionCost.Variable => "◆–◆◆◆",
            _ => "",
        };
    }
}
