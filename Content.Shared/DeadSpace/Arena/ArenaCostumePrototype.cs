using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Arena;

/// <summary>
/// Одежда для вкладки «Костюм» в меню арены. Покупается за валюту убийств.
/// </summary>
[Prototype]
public sealed partial class ArenaCostumePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string NameLoc = string.Empty;

    [DataField]
    public string DescLoc = string.Empty;

    /// <summary>Категория: cloak / jumpsuit / vest.</summary>
    [DataField]
    public string Category = string.Empty;

    /// <summary>Прототип одежды, который надевается на персонажа.</summary>
    [DataField]
    public EntProtoId Item;

    /// <summary>Слот инвентаря, в который надевается предмет (outerClothing / jumpsuit).</summary>
    [DataField]
    public string Slot = string.Empty;

    /// <summary>Цена в валюте убийств.</summary>
    [DataField]
    public int Price;
}
